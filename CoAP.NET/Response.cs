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

namespace CoAP
{
    /// <summary>
    /// This class describes the functionality of a CoAP Response as
    /// a subclass of a CoAP Message.
    /// </summary>
    public class Response : Message
    {
        private Request _request;

        /// <summary>
        /// Initializes a response message with default code.
        /// </summary>
        public Response()
            : this(CoAP.Code.Valid)
        { }

        /// <summary>
        /// Initializes a response message.
        /// </summary>
        /// <param name="code">The code of this response</param>
        public Response(Int32 code)
        {
            Code = code;
        }

        /// <summary>
        /// Gets or sets the request related to this response.
        /// </summary>
        public Request Request
        {
            get { return _request; }
            set { _request = value; }
        }

        /// <summary>
        /// Gets the Round-Trip Time of this response.
        /// </summary>
        public Double RTT
        {
            get
            {
                if (null == _request)
                    return -1D;
                else
                {
                    return new TimeSpan(Timestamp - _request.Timestamp).TotalMilliseconds;
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether this response is a "Piggy-backed" response,
        /// which is carried directly in the acknowledgement message.
        /// </summary>
        public Boolean IsPiggyBacked
        {
            get
            {
                return IsAcknowledgement && Code != CoAP.Code.Empty;
            }
        }

        protected override void DoHandleBy(IMessageHandler handler)
        {
            handler.HandleMessage(this);
        }

        protected override void PayloadAppended(Byte[] block)
        {
            if (null != _request)
                _request.ResponsePayloadAppended(this, block);
        }
    }
}
