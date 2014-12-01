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

namespace CoAP.Log
{
    /// <summary>
    /// Provides methods to log messages.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Is debug enabled?
        /// </summary>
        Boolean IsDebugEnabled { get; }
        /// <summary>
        /// Is error enabled?
        /// </summary>
        Boolean IsErrorEnabled { get; }
        /// <summary>
        /// Is fatal enabled?
        /// </summary>
        Boolean IsFatalEnabled { get; }
        /// <summary>
        /// Is info enabled?
        /// </summary>
        Boolean IsInfoEnabled { get; }
        /// <summary>
        /// Is warning enabled?
        /// </summary>
        Boolean IsWarnEnabled { get; }
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        void Debug(Object message);
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        void Debug(Object message, Exception exception);
        /// <summary>
        /// Logs an error message.
        /// </summary>
        void Error(Object message);
        /// <summary>
        /// Logs an error message.
        /// </summary>
        void Error(Object message, Exception exception);
        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        void Fatal(Object message);
        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        void Fatal(Object message, Exception exception);
        /// <summary>
        /// Logs an info message.
        /// </summary>
        void Info(Object message);
        /// <summary>
        /// Logs an info message.
        /// </summary>
        void Info(Object message, Exception exception);
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void Warn(Object message);
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void Warn(Object message, Exception exception);
    }
}
