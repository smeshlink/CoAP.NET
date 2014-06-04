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
using CoAP.Net;

namespace CoAP.Server.Resources
{
    public abstract class ForwardingResource : Resource
    {
        public ForwardingResource(String resourceIdentifier)
            : base(resourceIdentifier)
        { }

        public ForwardingResource(String resourceIdentifier, Boolean hidden)
            : base(resourceIdentifier, hidden)
        { }

        public override void HandleRequest(Exchange exchange)
        {
            exchange.SendAccept();
            Response response = ForwardRequest(exchange.Request);
            exchange.SendResponse(response);
        }

        protected abstract Response ForwardRequest(Request request);
    }
}
