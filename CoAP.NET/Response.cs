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

namespace CoAP
{
    /// <summary>
    /// Represents a CoAP response to a CoAP request.
    /// A response is either a piggy-backed response with type ACK
    /// or a separate response with type CON or NON.
    /// </summary>
    public class Response : Message
    {
        private readonly StatusCode _statusCode;
        private Request _request;
        private Double _rtt;
        private Boolean _last = true;

        /// <summary>
        /// Initializes a response message.
        /// </summary>
        /// <param name="code">The code of this response</param>
        public Response(StatusCode code)
            : base(MessageType.Unknown, (Int32)code)
        {
            _statusCode = code;
        }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public StatusCode StatusCode
        {
            get { return _statusCode; }
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
            get { return _rtt; }
            set { _rtt = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this response is the last response of an exchange.
        /// </summary>
        public Boolean Last
        {
            get { return _last; }
            set { _last = value; }
        }

        /// <summary>
        /// Creates a piggy-backed response with the specified response code
        /// to the specified request.
        /// </summary>
        public static Response CreatePiggybackedResponse(Request request, StatusCode code)
        {
            Response response = new Response(code);
            response.ID = request.ID;
            response.Type = MessageType.ACK;
            response.Destination = request.Source;
            response.Token = request.Token;
            return response;
        }

        /// <summary>
        /// Creates a separate response with the specified response code to the
	    /// specified request. The destination address of the response is the source
	    /// address of the request. The response has the same token as the request
        /// but needs another MID from the CoAP network stack.
        /// </summary>
        public static Response CreateSeparateResponse(Request request, StatusCode code)
        {
            Response response = new Response(code);
            response.Destination = request.Source;
            response.Token = request.Token;
            return response;
        }
    }
}
