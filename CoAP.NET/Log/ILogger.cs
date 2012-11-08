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
    interface ILogger
    {
        Boolean IsDebugEnabled { get; }
        Boolean IsErrorEnabled { get; }
        Boolean IsFatalEnabled { get; }
        Boolean IsInfoEnabled { get; }
        Boolean IsWarnEnabled { get; }
        void Debug(Object message);
        void Debug(Object message, Exception exception);
        void Error(Object message);
        void Error(Object message, Exception exception);
        void Fatal(Object message);
        void Fatal(Object message, Exception exception);
        void Info(Object message);
        void Info(Object message, Exception exception);
        void Warn(Object message);
        void Warn(Object message, Exception exception);
    }
}
