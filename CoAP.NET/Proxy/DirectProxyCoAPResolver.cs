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

using CoAP.Net;
using CoAP.Proxy.Resources;

namespace CoAP.Proxy
{
    public class DirectProxyCoAPResolver : IProxyCoAPResolver
    {
        private ForwardingResource _proxyCoapClientResource;

        public DirectProxyCoAPResolver()
        { }

        public DirectProxyCoAPResolver(ForwardingResource proxyCoapClientResource)
        {
            _proxyCoapClientResource = proxyCoapClientResource;
        }

        public ForwardingResource ProxyCoapClientResource
        {
            get { return _proxyCoapClientResource; }
            set { _proxyCoapClientResource = value; }
        }

        public void ForwardRequest(Exchange exchange)
        {
            _proxyCoapClientResource.HandleRequest(exchange);
        }
    }
}
