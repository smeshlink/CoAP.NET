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
using System.Net;
using CoAP.Log;
using CoAP.Server.Resources;

namespace CoAP.Proxy.Resources
{
    public class ProxyHttpClientResource : ForwardingResource
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(ProxyHttpClientResource));

        public ProxyHttpClientResource()
            : this("proxy/coapClient")
        { }

        public ProxyHttpClientResource(String name)
            : base(name)
        {
            Attributes.Title = "Forward the requests to a HTTP client.";
        }

        protected override Response ForwardRequest(Request incomingCoapRequest)
        {
            // check the invariant: the request must have the proxy-uri set
            if (!incomingCoapRequest.HasOption(OptionType.ProxyUri))
            {
                if (log.IsWarnEnabled)
                    log.Warn("Proxy-uri option not set.");
                return new Response(StatusCode.BadOption);
            }

            // remove the fake uri-path
            incomingCoapRequest.RemoveOptions(OptionType.UriPath); // HACK

            // get the proxy-uri set in the incoming coap request
            Uri proxyUri;
            try
            {
                proxyUri = incomingCoapRequest.ProxyUri;
            }
            catch (UriFormatException e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Proxy-uri option malformed: " + e.Message);
                return new Response(StatusCode.BadOption);
            }

            WebRequest httpRequest = null;
            try
            {
                httpRequest = HttpTranslator.GetHttpRequest(incomingCoapRequest);
            }
            catch (TranslationException e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Problems during the http/coap translation: " + e.Message);
                return new Response(StatusCode.BadGateway);
            }

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            DateTime timestamp = DateTime.Now;
            try
            {
                Response coapResponse = HttpTranslator.GetCoapResponse(httpResponse, incomingCoapRequest);
                coapResponse.Timestamp = timestamp;
                return coapResponse;
            }
            catch (TranslationException e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Problems during the http/coap translation: " + e.Message);
                return new Response(StatusCode.BadGateway);
            }
        }
    }
}
