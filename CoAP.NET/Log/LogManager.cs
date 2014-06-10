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
    public static class LogManager
    {
        static LogLevel _level = LogLevel.All;
        static ILogManager _manager;

        static LogManager()
        {
            Type test;
            try
            {
                test = Type.GetType("Common.Logging.LogManager, Common.Logging");
            }
            catch
            {
                test = null;
            }

            if (test == null)
                _manager = new ConsoleLogManager();
            else
                _manager = new CommonLoggingManager();
        }

        public static LogLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ILogManager"/> to provide loggers.
        /// </summary>
        public static ILogManager Instance
        {
            get { return _manager; }
            set { _manager = value ?? NopLogManager.Instance; }
        }

        public static ILogger GetLogger(Type type)
        {
            return _manager.GetLogger(type);
        }

        public static ILogger GetLogger(String name)
        {
            return _manager.GetLogger(name);
        }
    }

    public enum LogLevel
    {
        All,
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
        None
    }
}
