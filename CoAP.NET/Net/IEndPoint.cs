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

namespace CoAP.Net
{
    /// <summary>
    /// Represents a communication endpoint multiplexing CoAP message exchanges
    /// between (potentially multiple) clients and servers.
    /// </summary>
    public interface IEndPoint : IDisposable
    {
        /// <summary>
        /// Gets this endpoint's configuration.
        /// </summary>
        ICoapConfig Config { get; }
        /// <summary>
        /// Gets the local <see cref="System.Net.EndPoint"/> this endpoint is associated with.
        /// </summary>
        System.Net.EndPoint LocalEndPoint { get; }
        /// <summary>
        /// Checks if the endpoint has started.
        /// </summary>
        Boolean Running { get; }
        /// <summary>
        /// Starts this endpoint and all its components.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops this endpoint and all its components
        /// </summary>
        void Stop();
        void Clear();
        /// <summary>
        /// Sends the specified request.
        /// </summary>
        /// <param name="request"></param>
        void SendRequest(Request request);
        /// <summary>
        /// Sends the specified response.
        /// </summary>
        void SendResponse(Exchange exchange, Response response);
        /// <summary>
        /// Sends the specified empty message.
        /// </summary>
        void SendEmptyMessage(Exchange exchange, EmptyMessage message);
    }
}
