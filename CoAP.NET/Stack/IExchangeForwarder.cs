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

namespace CoAP.Stack
{
    public interface IExchangeForwarder
    {
        void SendRequest(Exchange exchange, Request request);
        void SendResponse(Exchange exchange, Response response);
        void SendEmptyMessage(Exchange exchange, EmptyMessage message);
    }
}
