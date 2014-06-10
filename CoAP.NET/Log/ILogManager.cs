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

namespace CoAP.Log
{
    /// <summary>
    /// Provides methods to acquire <see cref="CoAP.Log.ILogger"/>.
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// Gets a logger of the given type.
        /// </summary>
        ILogger GetLogger(Type type);
        /// <summary>
        /// Gets a named logger.
        /// </summary>
        ILogger GetLogger(String name);
    }
}
