/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;

namespace CoAP.Util
{
    /// <summary>
    /// This class implements a simple way for logging events in the CoAP library.
    /// </summary>
    public static class Log
    {
        private static ILogger _logger = new ConsoleLogger(Console.Out);

        /// <summary>
        /// Gets a value indicating whether debug log is enabled.
        /// </summary>
        public static Boolean IsDebugEnabled
        {
            get { return _logger.IsDebugEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether info log is enabled.
        /// </summary>
        public static Boolean IsInfoEnabled
        {
            get { return _logger.IsInfoEnabled; ; }
        }

        /// <summary>
        /// Gets a value indicating whether warning log is enabled.
        /// </summary>
        public static Boolean IsWarningEnabled
        {
            get { return _logger.IsWarningEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether error log is enabled.
        /// </summary>
        public static Boolean IsErrorEnabled
        {
            get { return _logger.IsErrorEnabled; }
        }

        /// <summary>
        /// Logs an debug event with the specified message.
        /// </summary>
        /// <param name="sender">The object the event originated from</param>
        /// <param name="msg">A string describing the event</param>
        /// <param name="args">Arguments referenced by the format specifiers in the message string</param>
        public static void Debug(Object sender, String msg, params Object[] args)
        {
            _logger.Debug(sender, msg, args);
        }

        /// <summary>
        /// Logs an error event with the specified message.
        /// </summary>
        /// <param name="sender">The object the event originated from</param>
        /// <param name="msg">A string describing the event</param>
        /// <param name="args">Arguments referenced by the format specifiers in the message string</param>
        public static void Error(Object sender, String msg, params Object[] args)
        {
            _logger.Error(sender, msg, args);
        }

        /// <summary>
        /// Logs an warning event with the specified message.
        /// </summary>
        /// <param name="sender">The object the event originated from</param>
        /// <param name="msg">A string describing the event</param>
        /// <param name="args">Arguments referenced by the format specifiers in the message string</param>
        public static void Warning(Object sender, String msg, params Object[] args)
        {
            _logger.Warning(sender, msg, args);
        }

        /// <summary>
        /// Logs an info event with the specified message.
        /// </summary>
        /// <param name="sender">The object the event originated from</param>
        /// <param name="msg">A string describing the event</param>
        /// <param name="args">Arguments referenced by the format specifiers in the message string</param>
        public static void Info(Object sender, String msg, params Object[] args)
        {
            _logger.Info(sender, msg, args);
        }

        interface ILogger
        {
            Boolean IsDebugEnabled { get; }
            Boolean IsInfoEnabled { get; }
            Boolean IsWarningEnabled { get; }
            Boolean IsErrorEnabled { get; }
            void Info(Object sender, String msg, params Object[] args);
            void Warning(Object sender, String msg, params Object[] args);
            void Error(Object sender, String msg, params Object[] args);
            void Debug(Object sender, String msg, params Object[] args);
        }

        class ConsoleLogger : ILogger
        {
            private System.IO.TextWriter _writer;

            public ConsoleLogger() : this(Console.Out) { }

            public ConsoleLogger(System.IO.TextWriter writer) 
            {
                _writer = writer;
            }

            #region ILogger 成员

            public Boolean IsDebugEnabled
            {
                get { return true; }
            }

            public Boolean IsInfoEnabled
            {
                get { return true; }
            }

            public Boolean IsWarningEnabled
            {
                get { return true; }
            }

            public Boolean IsErrorEnabled
            {
                get { return true; }
            }

            public void Error(Object sender, String msg, params Object[] args)
            {
                String format = String.Format("ERROR - {0}\n", msg);
                if (sender != null)
                {
                    format = "[" + sender.GetType().Name + "] " + format;
                }

                _writer.Write(format, args);
            }

            public void Warning(Object sender, String msg, params Object[] args)
            {
                String format = String.Format("WARNING - {0}\n", msg);
                if (sender != null)
                {
                    format = "[" + sender.GetType().Name + "] " + format;
                }

                _writer.Write(format, args);
            }

            public void Info(Object sender, String msg, params Object[] args)
            {
                String format = String.Format("INFO - {0}\n", msg);
                if (sender != null)
                {
                    format = "[" + sender.GetType().Name + "] " + format;
                }

                _writer.Write(format, args);
            }

            public void Debug(Object sender, String msg, params Object[] args)
            {
                String format = String.Format("DEBUG - {0}\n", msg);
                if (sender != null)
                {
                    format = "[" + sender.GetType().Name + "] " + format;
                }

                _writer.Write(format, args);
            }

            #endregion
        }
    }
}
