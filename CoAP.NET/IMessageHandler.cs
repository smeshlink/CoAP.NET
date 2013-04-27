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

namespace CoAP
{
    /// <summary>
    /// Provides methods to handle messages.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handles a request message.
        /// </summary>
        /// <param name="request">the request to handle</param>
        void HandleMessage(Request request);
        /// <summary>
        /// Handles a response message.
        /// </summary>
        /// <param name="Response">the response to handle</param>
        void HandleMessage(Response response);
    }
}
