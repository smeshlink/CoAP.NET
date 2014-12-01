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
    /// <summary>
    /// Base class for message decoders.
    /// </summary>
    public abstract class MessageDecoder : IMessageDecoder
    {
        /// <summary>
        /// the bytes reader
        /// </summary>
        protected DatagramReader _reader;
        /// <summary>
        /// the version of the decoding message
        /// </summary>
        protected Int32 _version;
        /// <summary>
        /// the type of the decoding message
        /// </summary>
        protected MessageType _type;
        /// <summary>
        /// the length of token
        /// </summary>
        protected Int32 _tokenLength;
        /// <summary>
        /// the code of the decoding message
        /// </summary>
        protected Int32 _code;
        /// <summary>
        /// the id of the decoding message
        /// </summary>
        protected Int32 _id;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="data">the bytes array to decode</param>
        public MessageDecoder(Byte[] data)
        {
            _reader = new DatagramReader(data);
        }

        /// <summary>
        /// Reads protocol headers.
        /// </summary>
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
            get { return _id; }
        }

        /// <inheritdoc/>
        public Request DecodeRequest()
        {
            System.Diagnostics.Debug.Assert(IsRequest);
            Request request = new Request((Method)_code);
            request.Type = _type;
            request.ID = _id;
            ParseMessage(request);
            return request;
        }

        /// <inheritdoc/>
        public Response DecodeResponse()
        {
            System.Diagnostics.Debug.Assert(IsResponse);
            Response response = new Response((StatusCode)_code);
            response.Type = _type;
            response.ID = _id;
            ParseMessage(response);
            return response;
        }

        /// <inheritdoc/>
        public EmptyMessage DecodeEmptyMessage()
        {
            System.Diagnostics.Debug.Assert(!IsRequest && !IsResponse);
            EmptyMessage message = new EmptyMessage(_type);
            message.Type = _type;
            message.ID = _id;
            ParseMessage(message);
            return message;
        }

        /// <inheritdoc/>
        public Message Decode()
        {
            if (IsRequest)
                return DecodeRequest();
            else if (IsResponse)
                return DecodeResponse();
            else if (IsEmpty)
                return DecodeEmptyMessage();
            else
                return null;
        }

        /// <summary>
        /// Parses the rest data other than protocol headers into the given message.
        /// </summary>
        /// <param name="message"></param>
        protected abstract void ParseMessage(Message message);
    }
}
