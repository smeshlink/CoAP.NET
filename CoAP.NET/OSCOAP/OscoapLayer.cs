using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CoAP.Log;
using CoAP.Net;
using CoAP.Stack;
using Com.AugustCellars.COSE;
using PeterO.Cbor;

namespace CoAP.OSCOAP
{
#if INCLUDE_OSCOAP
    public class OscoapLayer : AbstractLayer
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(OscoapLayer));
        static byte[] fixedHeader = new byte[] { 0x40, 0x01, 0xff, 0xff };


        /// <summary>
        /// Constructs a new OSCAP layer.
        /// </summary>
        public OscoapLayer(ICoapConfig config)
        {
            /*
            _maxMessageSize = config.MaxMessageSize;
            _defaultBlockSize = config.DefaultBlockSize;
            _blockTimeout = config.BlockwiseStatusLifetime;
            */
            if (log.IsDebugEnabled)
                log.Debug("OscoapLayer");  // Print out config if any

            config.PropertyChanged += ConfigChanged;
        }

        void ConfigChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ICoapConfig config = (ICoapConfig)sender;
            /*
            if (String.Equals(e.PropertyName, "MaxMessageSize"))
                _maxMessageSize = config.MaxMessageSize;
            else if (String.Equals(e.PropertyName, "DefaultBlockSize"))
                _defaultBlockSize = config.DefaultBlockSize;
            else if (String.Equals(e.PropertyName, "BlockwiseStatusLifetime"))
                _blockTimeout = config.BlockwiseStatusLifetime;
                */
        }

        public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if ((request.OscoapContext != null) || (exchange.OscoapContext != null)) {
                bool hasPayload = false;

                OSCOAP.SecurityContext ctx = exchange.OscoapContext;
                if (request.OscoapContext != null) {
                    ctx = request.OscoapContext;
                    exchange.OscoapContext = ctx;
                }

                Codec.IMessageEncoder me = Spec.Default.NewMessageEncoder();
                Request encryptedRequest = new Request(CoAP.Method.GET);

                if (request.Payload != null) {
                    hasPayload = true;
                    encryptedRequest.Payload = request.Payload;
                }

                MoveRequestHeaders(request, encryptedRequest);

                if (log.IsDebugEnabled) {
                    log.Debug("New inner response message");
                    log.Debug(encryptedRequest.ToString());
                }

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedRequest);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (4 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 0, msg2.Length);
                enc.SetContent(msg2);

                // Build the partial URI
                string partialURI = request.URI.AbsoluteUri; // M00BUG?

                // Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1));
                aad.Add(CBORObject.FromObject(request.Code));
                aad.Add(CBORObject.FromObject(ctx.Sender.Algorithm));
                aad.Add(CBORObject.FromObjectAndTag(partialURI, 32));

                enc.SetExternalData(aad.EncodeToBytes());
                enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.PROTECTED);
                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.KeyId, CBORObject.FromObject(ctx.Cid), Attributes.PROTECTED);

                enc.Encrypt(ctx.Sender.Key);

                if (hasPayload) {
                    request.Payload = enc.EncodeToBytes();
                    request.AddOption(new OSCOAP.OscoapOption());
                }
                else {
                    OSCOAP.OscoapOption o = new OSCOAP.OscoapOption();
                    o.Set(enc.EncodeToBytes());
                    request.AddOption(o);
                }
            }
            base.SendRequest(nextLayer, exchange, request);
        }

        public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (request.HasOption(OptionType.Oscoap))
            {
                try
                {
                    CoAP.Option op = request.GetFirstOption(OptionType.Oscoap);
                    request.RemoveOptions(OptionType.Oscoap);

                    Encrypt0Message msg;
                    if (op.RawValue.Length == 0)
                    {
                        msg = (Encrypt0Message) Com.AugustCellars.COSE.Message.DecodeFromBytes(request.Payload, Tags.Encrypted);
                    }
                    else
                    {
                        msg = (Encrypt0Message) Com.AugustCellars.COSE.Message.DecodeFromBytes(op.RawValue, Tags.Encrypted);
                    }

                    List<SecurityContext> contexts = new List<SecurityContext>();
                    SecurityContext ctx = null;

                    if (exchange.OscoapContext != null) {
                        contexts.Add(exchange.OscoapContext);
                    }
                    else {
                        CBORObject kid = msg.FindAttribute(HeaderKeys.KeyId);
                        contexts = OSCOAP.SecurityContextSet.AllContexts.FindByKid(kid.GetByteString());
                        if (contexts.Count == 0) return;  // Ignore messages that have no known security context.
                    }

                    String partialURI = request.URI.AbsoluteUri; // M00BUG?

                    //  Build AAD
                    CBORObject aad = CBORObject.NewArray();
                    aad.Add(CBORObject.FromObject(1)); // M00BUG
                    aad.Add(CBORObject.FromObject(request.Code));
                    aad.Add(CBORObject.FromObject(0));
                    aad.Add(CBORObject.FromObjectAndTag(partialURI, 32));

                    byte[] payload = null;
                    byte[] partialIV = msg.FindAttribute(HeaderKeys.PartialIV).GetByteString();
                    byte[] seqNoArray = new byte[8];
                    Array.Copy(partialIV, 0, seqNoArray, 8 - partialIV.Length, partialIV.Length);
                    if (BitConverter.IsLittleEndian) Array.Reverse(seqNoArray);
                    Int64 seqNo = BitConverter.ToInt64(seqNoArray, 0);

                    foreach (SecurityContext context in contexts) {
                        if (context.Recipient.ReplayWindow.HitTest(seqNo)) continue;

                        aad[2] = context.Recipient.Algorithm;

                        msg.SetExternalData(aad.EncodeToBytes());

                        msg.AddAttribute(HeaderKeys.Algorithm, context.Recipient.Algorithm, Attributes.DO_NOT_SEND);
                        msg.AddAttribute(HeaderKeys.IV, context.Recipient.GetIV(partialIV), Attributes.DO_NOT_SEND);
 
                        try {
                            ctx = context;
                            payload = msg.Decrypt(context.Recipient.Key);
                            context.Recipient.ReplayWindow.SetHit(seqNo);
                        }
                        catch (Exception) {
                            ctx = null;
                        }

                        if (ctx != null) {
                            break;
                        }
                    }

                    exchange.OscoapContext = ctx;  // So we know it on the way back.
                    request.OscoapContext = ctx;
                    exchange.OscoapSequenceNumber = partialIV;

                    byte[] newRequestData = new byte[payload.Length + fixedHeader.Length];
                    Array.Copy(fixedHeader, newRequestData, fixedHeader.Length);
                    Array.Copy(payload, 0, newRequestData, fixedHeader.Length, payload.Length);

                    CoAP.Codec.IMessageDecoder me = CoAP.Spec.Default.NewMessageDecoder(newRequestData);
                    CoAP.Request newRequest = me.DecodeRequest();

                    //  Update headers is a pain

                    RestoreOptions(request, newRequest);

                    request.Payload = newRequest.Payload;
                }
                catch (Exception e)
                {
                    log.Error("OSCOAP Layer: reject message because " + e.ToString());
                    exchange.OscoapContext = null;
                    //  Ignore messages that we cannot decrypt.
                    return;
                }
            }

            base.ReceiveRequest(nextLayer, exchange, request);
        }

        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (exchange.OscoapContext != null)
            {
                OSCOAP.SecurityContext ctx = exchange.OscoapContext;

                Codec.IMessageEncoder me = Spec.Default.NewMessageEncoder();
                Response encryptedResponse = new Response((CoAP.StatusCode) response.Code);

                bool hasPayload = false;
                if (response.Payload != null)
                {
                    hasPayload = true;
                    encryptedResponse.Payload = response.Payload;
                }

                MoveResponseHeaders(response, encryptedResponse);

                if (log.IsDebugEnabled) {
                    log.Debug("New inner response message");
                    log.Debug(encryptedResponse.ToString());
                }

                //  Build AAD
                CBORObject aad = CBORObject.NewArray();
                aad.Add(1);
                aad.Add(response.Code);
                aad.Add(ctx.Sender.Algorithm);
                aad.Add(ctx.Cid);
                aad.Add(ctx.Recipient.Id);
                aad.Add(exchange.OscoapSequenceNumber);

                Encrypt0Message enc = new Encrypt0Message(false);
                byte[] msg = me.Encode(encryptedResponse);
                int tokenSize = msg[0] & 0xf;
                byte[] msg2 = new byte[msg.Length - (4 + tokenSize)];
                Array.Copy(msg, 4 + tokenSize, msg2, 0, msg2.Length);
                enc.SetContent(msg2);
                enc.SetExternalData(aad.EncodeToBytes());

                enc.AddAttribute(HeaderKeys.IV, ctx.Sender.GetIV(ctx.Sender.PartialIV), Attributes.DO_NOT_SEND);
                enc.AddAttribute(HeaderKeys.PartialIV, CBORObject.FromObject(ctx.Sender.PartialIV), Attributes.PROTECTED);
                enc.AddAttribute(HeaderKeys.Algorithm, ctx.Sender.Algorithm, Attributes.DO_NOT_SEND);
                enc.Encrypt(ctx.Sender.Key);

                if (hasPayload)
                {
                    response.Payload = enc.EncodeToBytes();
                    response.AddOption(new OSCOAP.OscoapOption());
                }
                else
                {
                    OSCOAP.OscoapOption o = new OSCOAP.OscoapOption();
                    o.Set(enc.EncodeToBytes());
                    response.AddOption(o);
                }
            }

            base.SendResponse(nextLayer, exchange, response);
        }

        public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            if (response.HasOption(OptionType.Oscoap))
            {
                Encrypt0Message msg;
                OSCOAP.SecurityContext ctx;
                Option op = response.GetFirstOption(OptionType.Oscoap);

                if (op.RawValue.Length > 0)
                {
                    msg = (Encrypt0Message) Com.AugustCellars.COSE.Message.DecodeFromBytes(op.RawValue, Tags.Encrypted);
                }
                else
                {
                    msg = (Encrypt0Message) Com.AugustCellars.COSE.Message.DecodeFromBytes(response.Payload, Tags.Encrypted);
                }


                if (exchange.OscoapContext == null)
                {
                    return;
                }
                else ctx = exchange.OscoapContext;

                msg.AddAttribute(HeaderKeys.Algorithm, ctx.Recipient.Algorithm, Attributes.DO_NOT_SEND);
                msg.AddAttribute(HeaderKeys.IV, ctx.Recipient.GetIV(msg.FindAttribute(HeaderKeys.PartialIV)), Attributes.DO_NOT_SEND);

                //  build aad
                CBORObject aad = CBORObject.NewArray();
                aad.Add(CBORObject.FromObject(1));
                aad.Add(CBORObject.FromObject(response.Code));
                aad.Add(ctx.Recipient.Algorithm);
                aad.Add(ctx.Cid);
                aad.Add(ctx.Sender.Id);
                aad.Add(ctx.Sender.PartialIV);
                msg.SetExternalData(aad.EncodeToBytes());

                byte[] payload = msg.Decrypt(ctx.Recipient.Key);
                byte[] rgb = new byte[payload.Length + fixedHeader.Length];
                Array.Copy(fixedHeader, rgb, fixedHeader.Length);
                Array.Copy(payload,0, rgb, fixedHeader.Length, payload.Length);
                rgb[1] = 0x45;
                Codec.IMessageDecoder me = CoAP.Spec.Default.NewMessageDecoder(rgb);
                Response decryptedReq = me.DecodeResponse();

                response.Payload = decryptedReq.Payload;

                RestoreOptions(response, decryptedReq);
            }
            base.ReceiveResponse(nextLayer, exchange, response);
        }

        void MoveRequestHeaders(Request unprotected, Request encrypted)
        {
            List<Option> deleteMe = new List<Option>();
            int port;

            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                if (!unprotected.ProxyUri.IsAbsoluteUri) throw new Exception("Must be an absolute URI");
                if (unprotected.ProxyUri.Fragment != null) throw new Exception("Fragments not allowed in ProxyUri");
                switch (unprotected.ProxyUri.Scheme) {
                    case "coap":
                    port = 5683;
                    break;

                    case "coaps":
                    port = 5684;
                    break;

                    default:
                    throw new Exception("Unsupported schema");
                }

                if (unprotected.ProxyUri.Query != null) {
                    encrypted.AddUriQuery(unprotected.ProxyUri.Query);
                    unprotected.ClearUriQuery();
                }

                if (unprotected.ProxyUri.Host[0] != '[') {
                    encrypted.UriHost = unprotected.ProxyUri.Host;
                }

                String strPort = "";
                if ((unprotected.ProxyUri.Port != 0) && (unprotected.ProxyUri.Port != port)) {
                    encrypted.UriPort = unprotected.ProxyUri.Port;
                    strPort = ":" + port.ToString();
                }

                string p = unprotected.ProxyUri.AbsolutePath;
                if (p != null) {
                    encrypted.UriPath = p;
                }
                unprotected.URI = new Uri(unprotected.ProxyUri.Scheme + "://" + unprotected.ProxyUri.Host + strPort + "/");
            }

            List<Option> toDelete = new List<Option>();
            foreach (Option op in unprotected.GetOptions()) {
                switch (op.Type) {
                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                        break;

                    case OptionType.Observe:
                        encrypted.AddOption(op);
                        break;

                    default:
                        encrypted.AddOption(op);
                        toDelete.Add(op);
                        break;
                }
            }

            foreach (Option op in toDelete) unprotected.RemoveOptions(op.Type);
            unprotected.URI = null;
        }

        void MoveResponseHeaders(Response unprotected, Response encrypted)
        {
            List<Option> deleteMe = new List<Option>();

            //  Deal with Proxy-Uri
            if (unprotected.ProxyUri != null) {
                throw new Exception("Should not see Proxy-Uri on a response.");
            }

            List<Option> toDelete = new List<Option>();
            foreach (Option op in unprotected.GetOptions()) {
                switch (op.Type) {
                    case OptionType.UriHost:
                    case OptionType.UriPort:
                    case OptionType.ProxyUri:
                    case OptionType.ProxyScheme:
                        break;

                    case OptionType.Observe:
                        encrypted.AddOption(op);
                        break;

                    default:
                        encrypted.AddOption(op);
                        toDelete.Add(op);
                        break;
                }
            }

            foreach (Option op in toDelete) unprotected.RemoveOptions(op.Type);
        }

 void                        RestoreOptions(Message response, Message decryptedReq)
        {
            foreach (Option op in response.GetOptions()) {
                switch (op.Type) {
                    case OptionType.Block1:
                    case OptionType.Block2:
                    case OptionType.Oscoap:
                        response.RemoveOptions(op.Type);
                        break;

                    default:
                        break;
                }
            }

            foreach (Option op in decryptedReq.GetOptions()) {
                switch (op.Type) {
                    default:
                        response.AddOption(op);
                        break;
                }
            }
        }

    }
#endif
}
