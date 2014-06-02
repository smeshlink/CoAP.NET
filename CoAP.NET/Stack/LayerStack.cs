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
    /// Stack of layers.
    /// </summary>
    public class LayerStack : Chain<LayerStack, ILayer, INextLayer>
    {
        public LayerStack()
            : base(
            e => new NextLayer(e),
            () => new StackTopLayer(), () => new StackBottomLayer()
            )
        { }

        public void SendRequest(Request request)
        {
            Head.Filter.SendRequest(Head.NextFilter, null, request);
        }

        public void SendResponse(Exchange exchange, Response response)
        {
            Head.Filter.SendResponse(Head.NextFilter, exchange, response);
        }

        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            Head.Filter.SendEmptyMessage(Head.NextFilter, exchange, message);
        }

        public void ReceiveRequest(Exchange exchange, Request request)
        {
            Tail.Filter.ReceiveRequest(Head.NextFilter, exchange, request);
        }

        public void ReceiveResponse(Exchange exchange, Response response)
        {
            Tail.Filter.ReceiveResponse(Head.NextFilter, exchange, response);
        }

        public void ReceiveEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            Tail.Filter.ReceiveEmptyMessage(Head.NextFilter, exchange, message);
        }

        class StackTopLayer : AbstractLayer
        {
            public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
            {
                if (exchange == null)
                    exchange = new Exchange(request, Origin.Local);
                exchange.Request = request;
                base.SendRequest(nextLayer, exchange, request);
            }

            public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
            {
                exchange.Response = response;
                base.SendResponse(nextLayer, exchange, response);
            }

            public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
            {
                // if there is no BlockwiseLayer we still have to set it
                if (exchange.Request == null)
                    exchange.Request = request;
                if (exchange.Deliverer != null)
                    exchange.Deliverer.DeliverRequest(exchange);
            }

            public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
            {
                if (!response.HasOption(OptionType.Observe))
                    exchange.Complete();
                if (exchange.Deliverer != null)
                    // notify request that response has arrived
                    exchange.Deliverer.DeliverResponse(exchange, response);
            }

            public override void ReceiveEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
            {
                // When empty messages reach the top of the CoAP stack we can ignore them. 
            }
        }

        class StackBottomLayer : AbstractLayer
        {
            public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
            {
                exchange.Forwarder.SendRequest(exchange, request);
            }

            public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
            {
                exchange.Forwarder.SendResponse(exchange, response);
            }

            public override void SendEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
            {
                exchange.Forwarder.SendEmptyMessage(exchange, message);
            }
        }

        class NextLayer : INextLayer
        {
            readonly Entry _entry;

            public NextLayer(Entry entry)
            {
                _entry = entry;
            }

            public void SendRequest(Exchange exchange, Request request)
            {
                _entry.NextEntry.Filter.SendRequest(_entry.NextEntry.NextFilter, exchange, request);
            }

            public void SendResponse(Exchange exchange, Response response)
            {
                _entry.NextEntry.Filter.SendResponse(_entry.NextEntry.NextFilter, exchange, response);
            }

            public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
            {
                _entry.PrevEntry.Filter.SendEmptyMessage(_entry.PrevEntry.NextFilter, exchange, message);
            }

            public void ReceiveRequest(Exchange exchange, Request request)
            {
                _entry.PrevEntry.Filter.ReceiveRequest(_entry.PrevEntry.NextFilter, exchange, request);
            }

            public void ReceiveResponse(Exchange exchange, Response response)
            {
                _entry.PrevEntry.Filter.ReceiveResponse(_entry.PrevEntry.NextFilter, exchange, response);
            }

            public void ReceiveEmptyMessage(Exchange exchange, EmptyMessage message)
            {
                _entry.PrevEntry.Filter.ReceiveEmptyMessage(_entry.PrevEntry.NextFilter, exchange, message);
            }
        }
    }
}
