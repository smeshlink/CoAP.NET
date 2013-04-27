/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
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
    /// Represents an event when a response arrives for a request.
    /// </summary>
    public class ResponseEventArgs : EventArgs
    {
        private Response _response;

        /// <summary>
        /// 
        /// </summary>
        public ResponseEventArgs(Response response)
        {
            _response = response;
        }

        /// <summary>
        /// Gets the incoming response.
        /// </summary>
        public Response Response
        {
            get { return _response; }
        }
    }
}
