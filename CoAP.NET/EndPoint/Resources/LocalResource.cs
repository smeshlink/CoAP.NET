/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
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
    public class LocalResource : Resource, IRequestHandler
    {
        public LocalResource(String resourceIdentifier)
            : base(resourceIdentifier)
        { }

        public LocalResource(String resourceIdentifier, Boolean hidden)
            : base(resourceIdentifier, hidden)
        { }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected override Resource CreateInstance(String name)
        {
            return new LocalResource(name);
        }

        public virtual void DoGet(Request request)
        {
            request.Respond(Code.MethodNotAllowed);
        }

        public virtual void DoPost(Request request)
        {
            request.Respond(Code.MethodNotAllowed);
        }

        public virtual void DoPut(Request request)
        {
            request.Respond(Code.MethodNotAllowed);
        }

        public virtual void DoDelete(Request request)
        {
            request.Respond(Code.MethodNotAllowed);
        }

        protected void Changed()
        {
            ObservingManager.Instance.NotifyObservers(this);
        }

        protected override void DoCreateSubResource(Request request, String newIdentifier)
        {
            request.Respond(Code.Forbidden);
        }
    }
}
