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
    /// A <see cref="ILogManager"/> which always returns the unique instance of
    /// a direct NOP (no operation) logger.
    /// </summary>
    public sealed class NopLogManager : ILogManager
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly NopLogManager Instance = new NopLogManager();
        private static readonly NopLogger NOP = new NopLogger();

        private NopLogManager()
        { }

        /// <inheritdoc/>
        public ILogger GetLogger(Type type)
        {
            return NOP;
        }

        /// <inheritdoc/>
        public ILogger GetLogger(String name)
        {
            return NOP;
        }

        class NopLogger : ILogger
        {
            public Boolean IsDebugEnabled
            {
                get { return false; }
            }

            public Boolean IsErrorEnabled
            {
                get { return false; }
            }

            public Boolean IsFatalEnabled
            {
                get { return false; }
            }

            public Boolean IsInfoEnabled
            {
                get { return false; }
            }

            public Boolean IsWarnEnabled
            {
                get { return false; }
            }

            public void Debug(Object message)
            {
                // NOP
            }

            public void Debug(Object message, Exception exception)
            {
                // NOP
            }

            public void Error(Object message)
            {
                // NOP
            }

            public void Error(Object message, Exception exception)
            {
                // NOP
            }

            public void Fatal(Object message)
            {
                // NOP
            }

            public void Fatal(Object message, Exception exception)
            {
                // NOP
            }

            public void Info(Object message)
            {
                // NOP
            }

            public void Info(Object message, Exception exception)
            {
                // NOP
            }

            public void Warn(Object message)
            {
                // NOP
            }

            public void Warn(Object message, Exception exception)
            {
                // NOP
            }
        }
    }
}
