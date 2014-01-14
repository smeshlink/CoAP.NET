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

using System;

namespace CoAP.Util
{
    /// <summary>
    /// Provides the translations between the messages from the internal CoAP nodes and external ones.
    /// </summary>
    public static class CoapTranslator
    {
        /// <summary>
        /// Starting from an external CoAP request, the method fills a new request
	    /// for the internal CoAP nodes. Translates the proxy-uri option in the uri
	    /// of the new request and simply copies the options and the payload from the
        /// original request to the new one.
        /// </summary>
        /// <param name="incomingRequest">the original request</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">the <paramref name="incomingRequest"/> is null</exception>
        /// <exception cref="TranslationException"></exception>
        public static Request GetRequest(Request incomingRequest)
        {
            if (incomingRequest == null)
                ThrowHelper.ArgumentNullException("incomingRequest");

            Request outgoingRequest = new Request(incomingRequest.Code, incomingRequest.Type == MessageType.CON);

            // copy payload
            Byte[] payload = incomingRequest.Payload;
            outgoingRequest.Payload = payload;

            // get the uri address from the proxy-uri option
            Uri serverUri = null;
            try
            {
                serverUri = incomingRequest.ProxyUri;
            }
            catch (UriFormatException e)
            {
                throw new TranslationException("Cannot translate the server uri", e);
            }

            if (serverUri != null)
                outgoingRequest.URI = serverUri;

            // copy every option from the original message
            foreach (Option opt in incomingRequest.GetOptions())
            {
                // do not copy the proxy-uri option because it is not necessary in
                // the new message
                // do not copy the token option because it is a local option and
                // have to be assigned by the proper layer
                // do not copy the block* option because it is a local option and
                // have to be assigned by the proper layer
                // do not copy the uri-* options because they are already filled in
                // the new message
                if (opt.Type == OptionType.ProxyUri
                    || opt.Type == OptionType.UriHost
                    || opt.Type == OptionType.UriPath
                    || opt.Type == OptionType.UriPort
                    || opt.Type == OptionType.UriQuery
                    || opt.Type == OptionType.Block1
                    || opt.Type == OptionType.Block2)
                    continue;

                outgoingRequest.AddOption(opt);
            }

            return outgoingRequest;
        }

        /// <summary>
        /// Fills the new response with the response received from the internal CoAP
	    /// node. Simply copies the options and the payload from the forwarded
        /// response to the new one.
        /// </summary>
        /// <param name="incomingResponse">the forwarded request</param>
        /// <exception cref="ArgumentNullException">the <paramref name="incomingResponse"/> is null</exception>
        /// <returns></returns>
        public static Response GetResponse(Response incomingResponse)
        {
            if (incomingResponse == null)
                ThrowHelper.ArgumentNullException("incomingResponse");

            Response outgoingResponse = new Response(incomingResponse.Code);

            // copy payload
            byte[] payload = incomingResponse.Payload;
            outgoingResponse.Payload = payload;

            // copy every option
            foreach (Option opt in incomingResponse.GetOptions())
            {
                // do not copy the token option because it is a local option and
                // have to be assigned by the proper layer
                // do not copy the block* option because it is a local option and
                // have to be assigned by the proper layer
                if (opt.Type == OptionType.Block1
                    || opt.Type == OptionType.Block2
                    || opt.Type == OptionType.Token)
                    continue;

                outgoingResponse.AddOption(opt);
            }

            return outgoingResponse;
        }
    }
}
