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

namespace CoAP.EndPoint.Resources
{
    public abstract class ForwardingResource : LocalResource
    {
        public ForwardingResource(String resourceIdentifier)
            : base(resourceIdentifier)
        { }

        public ForwardingResource(String resourceIdentifier, Boolean hidden)
            : base(resourceIdentifier, hidden)
        { }

        public override void DoGet(CoAP.Request request)
        {
            Response response = ForwardRequest(request);
            request.Respond(response);
        }

        public override void DoPost(CoAP.Request request)
        {
            Response response = ForwardRequest(request);
            request.Respond(response);
        }

        public override void DoPut(Request request)
        {
            Response response = ForwardRequest(request);
            request.Respond(response);
        }

        public override void DoDelete(CoAP.Request request)
        {
            Response response = ForwardRequest(request);
            request.Respond(response);
        }

        protected abstract Response ForwardRequest(Request request);
    }
}
