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
using CoAP.EndPoint.Resources;

namespace CoAP.EndPoint
{
    /// <summary>
    /// Represent the container of the resources and the layers used by the proxy.
    /// </summary>
    public class ProxyEndpoint : LocalEndPoint
    {
        public ProxyEndpoint()
            : this(Spec.DefaultPort, 8080, 0)
        {
            AddResource(new ProxyCoapClientResource());
            AddResource(new ProxyHttpClientResource());
        }

        public ProxyEndpoint(Int32 udpPort, Int32 httpPort, Int32 transferBlockSize)
            : base(CommunicatorFactory.CreateCommunicator(udpPort, httpPort, transferBlockSize))
        {
            AddResource(new ProxyCoapClientResource());
            AddResource(new ProxyHttpClientResource());
        }

        protected override void DoHandleMessage(Request request)
        {
            // edit the request to be correctly forwarded if the proxy-uri is set
            if (request.HasOption(OptionType.ProxyUri))
            {
                try
                {
                    ManageProxyUriRequest(request);
                }
                catch (UriFormatException)
                {
                    request.Respond(Code.BadOption);
                    request.SendResponse();
                }
            }

            // handle the request as usual
            Execute(request);
        }

        private void ManageProxyUriRequest(Request request)
        {
            Uri proxyUri = request.ProxyUri;
            String clientPath;
            if (proxyUri.Scheme != null && proxyUri.Scheme.StartsWith("http"))
                clientPath = "proxy/httpClient";
            else
                clientPath = "proxy/coapClient";
            request.UriPath = clientPath;
        }
    }
}
