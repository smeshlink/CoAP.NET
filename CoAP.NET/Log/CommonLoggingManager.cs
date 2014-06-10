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
    class CommonLoggingManager : ILogManager
    {
        public ILogger GetLogger(Type type)
        {
            return new CommonLogging(Common.Logging.LogManager.GetLogger(type));
        }

        public ILogger GetLogger(String name)
        {
            return new CommonLogging(Common.Logging.LogManager.GetLogger(name));
        }

        class CommonLogging : ILogger
        {
            private readonly Common.Logging.ILog _log;

            public CommonLogging(Common.Logging.ILog log)
            {
                _log = log;
            }

            public void Debug(Object message, Exception exception)
            {
                _log.Debug(message, exception);
            }

            public void Debug(Object message)
            {
                _log.Debug(message);
            }

            public void DebugFormat(IFormatProvider provider, String format, params Object[] args)
            {
                _log.DebugFormat(provider, format, args);
            }

            public void DebugFormat(String format, Object arg0, Object arg1, Object arg2)
            {
                _log.DebugFormat(format, arg0, arg1, arg2);
            }

            public void DebugFormat(String format, Object arg0, Object arg1)
            {
                _log.DebugFormat(format, arg0, arg1);
            }

            public void DebugFormat(String format, Object arg0)
            {
                _log.DebugFormat(format, arg0);
            }

            public void DebugFormat(String format, params Object[] args)
            {
                _log.DebugFormat(format, args);
            }

            public void Error(Object message, Exception exception)
            {
                _log.Error(message, exception);
            }

            public void Error(Object message)
            {
                _log.Error(message);
            }

            public void ErrorFormat(IFormatProvider provider, String format, params Object[] args)
            {
                _log.ErrorFormat(provider, format, args);
            }

            public void ErrorFormat(String format, Object arg0, Object arg1, Object arg2)
            {
                _log.ErrorFormat(format, arg0, arg1, arg2);
            }

            public void ErrorFormat(String format, Object arg0, Object arg1)
            {
                _log.ErrorFormat(format, arg0, arg1);
            }

            public void ErrorFormat(String format, Object arg0)
            {
                _log.ErrorFormat(format, arg0);
            }

            public void ErrorFormat(String format, params Object[] args)
            {
                _log.ErrorFormat(format, args);
            }

            public void Fatal(Object message, Exception exception)
            {
                _log.Fatal(message, exception);
            }

            public void Fatal(Object message)
            {
                _log.Fatal(message);
            }

            public void FatalFormat(IFormatProvider provider, String format, params Object[] args)
            {
                _log.FatalFormat(provider, format, args);
            }

            public void FatalFormat(String format, Object arg0, Object arg1, Object arg2)
            {
                _log.FatalFormat(format, arg0, arg1, arg2);
            }

            public void FatalFormat(String format, Object arg0, Object arg1)
            {
                _log.FatalFormat(format, arg0, arg1);
            }

            public void FatalFormat(String format, Object arg0)
            {
                _log.FatalFormat(format, arg0);
            }

            public void FatalFormat(String format, params Object[] args)
            {
                _log.FatalFormat(format, args);
            }

            public void Info(Object message, Exception exception)
            {
                _log.Info(message, exception);
            }

            public void Info(Object message)
            {
                _log.Info(message);
            }

            public void InfoFormat(IFormatProvider provider, String format, params Object[] args)
            {
                _log.InfoFormat(provider, format, args);
            }

            public void InfoFormat(String format, Object arg0, Object arg1, Object arg2)
            {
                _log.InfoFormat(format, arg0, arg1, arg2);
            }

            public void InfoFormat(String format, Object arg0, Object arg1)
            {
                _log.InfoFormat(format, arg0, arg1);
            }

            public void InfoFormat(String format, Object arg0)
            {
                _log.InfoFormat(format, arg0);
            }

            public void InfoFormat(String format, params Object[] args)
            {
                _log.InfoFormat(format, args);
            }

            public Boolean IsDebugEnabled
            {
                get { return LogLevel.Debug >= LogManager.Level && _log.IsDebugEnabled; }
            }

            public Boolean IsErrorEnabled
            {
                get { return LogLevel.Error >= LogManager.Level && _log.IsErrorEnabled; }
            }

            public Boolean IsFatalEnabled
            {
                get { return LogLevel.Fatal >= LogManager.Level && _log.IsFatalEnabled; }
            }

            public Boolean IsInfoEnabled
            {
                get { return LogLevel.Info >= LogManager.Level && _log.IsInfoEnabled; }
            }

            public Boolean IsWarnEnabled
            {
                get { return LogLevel.Warning >= LogManager.Level && _log.IsWarnEnabled; }
            }

            public void Warn(Object message, Exception exception)
            {
                _log.Warn(message, exception);
            }

            public void Warn(Object message)
            {
                _log.Warn(message);
            }

            public void WarnFormat(IFormatProvider provider, String format, params Object[] args)
            {
                _log.WarnFormat(provider, format, args);
            }

            public void WarnFormat(String format, Object arg0, Object arg1, Object arg2)
            {
                _log.WarnFormat(format, arg0, arg1, arg2);
            }

            public void WarnFormat(String format, Object arg0, Object arg1)
            {
                _log.WarnFormat(format, arg0, arg1);
            }

            public void WarnFormat(String format, Object arg0)
            {
                _log.WarnFormat(format, arg0);
            }

            public void WarnFormat(String format, params Object[] args)
            {
                _log.WarnFormat(format, args);
            }
        }
    }
}
