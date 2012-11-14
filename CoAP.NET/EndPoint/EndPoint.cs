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

namespace CoAP.EndPoint
{
    public abstract class EndPoint : IMessageReceiver, IMessageHandler
    {
        public void ReceiveMessage(Message msg)
        {
            msg.HandleBy(this);
        }

        public void HandleMessage(Request request)
        {
            DoHandleMessage(request);
        }

        public void HandleMessage(Response response)
        {
            DoHandleMessage(response);
        }

        public void Execute(Request request)
        {
            DoExecute(request);
        }

        protected abstract void DoHandleMessage(Request request);
        protected abstract void DoHandleMessage(Response response);
        protected abstract void DoExecute(Request request);
    }
}
