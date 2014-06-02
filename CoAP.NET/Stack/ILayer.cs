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
    public interface ILayer
    {
        void SendRequest(INextLayer nextLayer, Exchange exchange, Request request);
        void SendResponse(INextLayer nextLayer, Exchange exchange, Response response);
        void SendEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message);
        void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request);
        void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response);
        void ReceiveEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message);
    }

    public interface INextLayer
    {
        void SendRequest(Exchange exchange, Request request);
        void SendResponse(Exchange exchange, Response response);
        void SendEmptyMessage(Exchange exchange, EmptyMessage message);
        void ReceiveRequest(Exchange exchange, Request request);
        void ReceiveResponse(Exchange exchange, Response response);
        void ReceiveEmptyMessage(Exchange exchange, EmptyMessage message);
    }
}
