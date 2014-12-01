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
    /// Log manager.
    /// </summary>
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

        /// <summary>
        /// Gets or sets the global log level.
        /// </summary>
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

        /// <summary>
        /// Gets a logger for the given type.
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            return _manager.GetLogger(type);
        }

        /// <summary>
        /// Gets a logger for the given type name.
        /// </summary>
        public static ILogger GetLogger(String name)
        {
            return _manager.GetLogger(name);
        }
    }

    /// <summary>
    /// Log levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// All logs.
        /// </summary>
        All,
        /// <summary>
        /// Debugs and above.
        /// </summary>
        Debug,
        /// <summary>
        /// Infos and above.
        /// </summary>
        Info,
        /// <summary>
        /// Warnings and above.
        /// </summary>
        Warning,
        /// <summary>
        /// Errors and above.
        /// </summary>
        Error,
        /// <summary>
        /// Fatals only.
        /// </summary>
        Fatal,
        /// <summary>
        /// No logs.
        /// </summary>
        None
    }
}
