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

namespace CoAP.Codec
{
    public abstract class MessageDecoder : IMessageDecoder
    {
        private DatagramReader _reader;
        private Int32 _version;
        private MessageType _type;
        private Int32 _tokenlength;
        private Int32 _code;
        private Int32 _mid;

        public MessageDecoder(Byte[] data)
        {
            _reader = new DatagramReader(data);
        }

        protected abstract void ReadProtocol();

        /// <inheritdoc/>
        public abstract Boolean IsWellFormed { get; }

        /// <inheritdoc/>
        public Boolean IsReply
        {
            get { return _type == MessageType.ACK || _type == MessageType.RST; }
        }

        /// <inheritdoc/>
        public virtual Boolean IsRequest
        {
            get
            {
                return _code >= CoapConstants.RequestCodeLowerBound &&
                    _code <= CoapConstants.RequestCodeUpperBound;
            }
        }

        /// <inheritdoc/>
        public virtual Boolean IsResponse
        {
            get
            {
                return _code >= CoapConstants.ResponseCodeLowerBound &&
                  _code <= CoapConstants.ResponseCodeUpperBound;
            }
        }

        /// <inheritdoc/>
        public Boolean IsEmpty
        {
            get { return _code == Code.Empty; }
        }

        /// <inheritdoc/>
        public Int32 Version
        {
            get { return _version; }
        }

        /// <inheritdoc/>
        public Int32 ID
        {
            get { return _mid; }
        }

        /// <inheritdoc/>
        public Request ParseRequest()
        {
            System.Diagnostics.Debug.Assert(IsRequest);
            Request request = new Request(_code);
            ParseMessage(request);
            return request;
        }

        /// <inheritdoc/>
        public Response ParseResponse()
        {
            System.Diagnostics.Debug.Assert(IsResponse);
            Response response = new Response(_code);
            ParseMessage(response);
            return response;
        }

        /// <inheritdoc/>
        public EmptyMessage ParseEmptyMessage()
        {
            System.Diagnostics.Debug.Assert(!IsRequest && !IsResponse);
            EmptyMessage message = new EmptyMessage(_type);
            ParseMessage(message);
            return message;
        }

        protected abstract void ParseMessage(Message message);
    }
}
