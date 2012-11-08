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

namespace CoAP
{
    /// <summary>
    /// Interface of message handlers
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handles a request message.
        /// </summary>
        /// <param name="request">The request to be handled</param>
        void HandleMessage(Request request);
        /// <summary>
        /// Handles a response message.
        /// </summary>
        /// <param name="Response">The response to be handled</param>
        void HandleMessage(Response response);
    }
}
