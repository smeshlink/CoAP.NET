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
    /// Interface of request handlers
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Handles GET request.
        /// </summary>
        /// <param name="request"></param>
        void DoGet(Request request);
        /// <summary>
        /// Handles POST request.
        /// </summary>
        /// <param name="request"></param>
        void DoPost(Request request);
        /// <summary>
        /// Handles PUT request.
        /// </summary>
        /// <param name="request"></param>
        void DoPut(Request request);
        /// <summary>
        /// Handles DELETE request.
        /// </summary>
        /// <param name="request"></param>
        void DoDelete(Request request);
    }
}
