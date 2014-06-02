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
    /// <summary>
    /// Builds up the stack of CoAP layers
    /// that process the CoAP protocol.
    /// </summary>
    public class CoapStack
    {
        private LayerStack _stack = new LayerStack();

        public CoapStack(ICoapConfig config)
        {
            _stack.AddLast("Observe", new ObserveLayer(config));
            _stack.AddLast("Blockwise", new BlockwiseLayer(config));
            _stack.AddLast("Token", new TokenLayer(config));
            _stack.AddLast("Reliability", new ReliabilityLayer(config));
        }

        public LayerStack Layers
        {
            get { return _stack; }
        }

        public void SendRequest(Request request)
        {
            _stack.SendRequest(request);
        }

        public void SendResponse(Exchange exchange, Response response)
        {
            _stack.SendResponse(exchange, response);
        }

        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            _stack.SendEmptyMessage(exchange, message);
        }

        public void ReceiveRequest(Exchange exchange, Request request)
        {
            _stack.ReceiveRequest(exchange, request);
        }

        public void ReceiveResponse(Exchange exchange, Response response)
        {
            _stack.ReceiveResponse(exchange, response);
        }

        public void ReceiveEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            _stack.ReceiveEmptyMessage(exchange, message);
        }
    }
}
