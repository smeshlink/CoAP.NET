/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using CoAP.Log;
using CoAP.Util;

namespace CoAP.EndPoint.Resources
{
    public class ProxyCoapClientResource : ForwardingResource
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(ProxyCoapClientResource));

        public ProxyCoapClientResource()
            : base("proxy/coapClient", true)
        {
            Title = "Forward the requests to a CoAP server.";
        }

        protected override Response ForwardRequest(Request incomingRequest)
        {
            // check the invariant: the request must have the proxy-uri set
            if (!incomingRequest.HasOption(OptionType.ProxyUri))
            {
                if (log.IsWarnEnabled)
                    log.Warn("Proxy-uri option not set.");
                return new Response(Code.BadOption);
            }

            // remove the fake uri-path
            // FIXME: HACK
            incomingRequest.RemoveOptions(OptionType.UriPath);

            // create a new request to forward to the requested coap server
            Request outgoingRequest = null;

            try
            {
                outgoingRequest = CoapTranslator.GetRequest(incomingRequest);
                outgoingRequest.ResponseQueueEnabled = true;
                outgoingRequest.Token = TokenManager.Instance.AcquireToken();

                outgoingRequest.Execute();
                incomingRequest.Accept();
            }
            catch (TranslationException ex)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Proxy-uri option malformed: " + ex.Message);
                return new Response(Code.BadOption);
            }
            catch (System.IO.IOException ex)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Failed to execute request: " + ex.Message);
                return new Response(Code.InternalServerError);
            }

            // receive the response
            Response receivedResponse;

            try
            {
                receivedResponse = outgoingRequest.ReceiveResponse();
            }
            catch (System.Threading.ThreadInterruptedException ex)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Receiving of response interrupted: " + ex.Message);
                return new Response(Code.InternalServerError);
            }

            if (receivedResponse == null)
            {
                return new Response(Code.GatewayTimeout);
            }
            else
            {
                // create the real response for the original request
                Response outgoingResponse = CoapTranslator.GetResponse(receivedResponse);
                return outgoingResponse;
            }
        }
    }
}
