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
using CoAP.Channel;
using CoAP.Log;
using CoAP.Codec;
using CoAP.Stack;

namespace CoAP.Net
{
    public class CoAPEndpoint : IEndPoint, IExchangeForwarder
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(CoAPEndpoint));

        readonly ICoapConfig _config;
        readonly IChannel _channel;
        readonly CoapStack _coapStack;
        private IMatcher _matcher;

        /// <summary>
        /// Instantiates a new endpoint.
        /// </summary>
        public CoAPEndpoint()
            : this(0, new CoapConfig())
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified port and configuration.
        /// </summary>
        public CoAPEndpoint(Int32 port, ICoapConfig config)
            : this(NewUDPChannel(port, config), config)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified <see cref="System.Net.EndPoint"/> and configuration.
        /// </summary>
        public CoAPEndpoint(System.Net.EndPoint localEP, ICoapConfig config)
            : this(NewUDPChannel(localEP, config), config)
        { }

        /// <summary>
        /// Instantiates a new endpoint with the
        /// specified channel and configuration.
        /// </summary>
        public CoAPEndpoint(IChannel channel, ICoapConfig config)
        {
            _config = config;
            _channel = channel;
            _matcher = new Matcher(config);
            _coapStack = new CoapStack(config);
            _channel.DataReceived += ReceiveData;
#if COAPALL
            _spec = config.Spec;
#endif
        }

#if COAPALL
        private ISpec _spec;

        public ISpec Spec
        {
            get { return _spec; }
            set { _spec = value; }
        }
#endif

        private void ReceiveData(Object sender, DataReceivedEventArgs e)
        {
            // TODO new thread
            // TODO may have more or less than one message in the incoming bytes

            IMessageDecoder parser = Spec.NewDataParser(e.Data);
            if (parser.IsRequest)
            {
                Request request;
                try
                {
                    request = parser.ParseRequest();
                }
                catch (InvalidOperationException)
                {
                    if (parser.IsReply)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("Message format error caused by " + e.EndPoint);
                    }
                    else
                    {
                        // manually build RST from raw information
                        EmptyMessage rst = new EmptyMessage(MessageType.RST);
                        rst.Destination = e.EndPoint;
                        rst.ID = parser.ID;
                        _channel.Send(Serialize(rst), rst.Destination);

                        if (log.IsWarnEnabled)
                            log.Warn("Message format error caused by " + e.EndPoint + " and reseted.");
                    }
                    return;
                }

                request.Source = e.EndPoint;
                Exchange exchange = _matcher.ReceiveRequest(request);
                if (exchange != null)
                {
                    exchange.Forwarder = this;
                    _coapStack.ReceiveRequest(null, request);
                }
            }
            else if (parser.IsResponse)
            {
                Response response = parser.ParseResponse();
				response.Source = e.EndPoint;

				// TODO response.setRTT(System.currentTimeMillis() - exchange.getTimestamp());
                Exchange exchange = _matcher.ReceiveResponse(response);
                if (exchange != null)
                {
                    exchange.Forwarder = this;
                    _coapStack.ReceiveResponse(null, response);
                }
            }
            else if (parser.IsEmpty)
            {
                EmptyMessage message = parser.ParseEmptyMessage();
                message.Source = e.EndPoint;

                // CoAP Ping
                if (message.Type == MessageType.CON || message.Type == MessageType.NON)
                {
                    EmptyMessage rst = EmptyMessage.NewRST(message);

                    if (log.IsDebugEnabled)
                        log.Debug("Responding to ping by " + e.EndPoint);

                    _channel.Send(Serialize(rst), rst.Destination);
                }
                else
                {
                    Exchange exchange = _matcher.ReceiveEmptyMessage(message);
                    if (exchange != null)
                    {
                        exchange.Forwarder = this;
                        _coapStack.ReceiveEmptyMessage(null, message);
                    }
                }
            }
            else if (log.IsDebugEnabled)
            {
                log.Debug("Silently ignoring non-CoAP message from " + e.EndPoint);
            }
        }

        private Byte[] Serialize(EmptyMessage message)
        {
            Byte[] bytes = message.Bytes;
            if (bytes == null)
            {
                bytes = Spec.NewDataSerializer().Serialize(message);
                message.Bytes = bytes;
            }
            return bytes;
        }

        private Byte[] Serialize(Request request)
        {
            Byte[] bytes = request.Bytes;
            if (bytes == null)
            {
                bytes = Spec.NewDataSerializer().Serialize(request);
                request.Bytes = bytes;
            }
            return bytes;
        }

        private Byte[] Serialize(Response response)
        {
            Byte[] bytes = response.Bytes;
            if (bytes == null)
            {
                bytes = Spec.NewDataSerializer().Serialize(response);
                response.Bytes = bytes;
            }
            return bytes;
        }

        static IChannel NewUDPChannel(Int32 port, ICoapConfig config)
        {
            UDPChannel channel = new UDPChannel(port);
            // TODO config
            return channel;
        }

        static IChannel NewUDPChannel(System.Net.EndPoint localEP, ICoapConfig config)
        {
            UDPChannel channel = new UDPChannel(localEP);
            // TODO config
            return channel;
        }

        void IExchangeForwarder.SendRequest(Exchange exchange, Request request)
        {
            _matcher.SendRequest(exchange, request);

			if (!request.Canceled)
				_channel.Send(Serialize(request), request.Destination);
        }

        void IExchangeForwarder.SendResponse(Exchange exchange, Response response)
        {
            _matcher.SendResponse(exchange, response);

			if (!response.Canceled)
                _channel.Send(Serialize(response), response.Destination);
        }

        void IExchangeForwarder.SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            _matcher.SendEmptyMessage(exchange, message);

			if (!message.Canceled)
                _channel.Send(Serialize(message), message.Destination);
        }
    }
}
