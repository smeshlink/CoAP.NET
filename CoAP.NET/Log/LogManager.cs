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
    public static class LogManager
    {
        public static LogLevel Level { get; set; }

        public enum LogLevel
        { 
            All,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        internal static ILogger GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        internal static ILogger GetLogger(String name)
        {
            return new ConsoleLogger();
        }

        class ConsoleLogger : ILogger
        {
            private System.IO.TextWriter _writer;

            public ConsoleLogger() : this(Console.Out) { }

            public ConsoleLogger(System.IO.TextWriter writer)
            {
                _writer = writer;
            }

            public Boolean IsDebugEnabled
            {
                get { return LogLevel.Debug >= Level; }
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

            public Boolean IsFatalEnabled
            {
                get { return true; }
            }

            public Boolean IsWarnEnabled
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

            public void Debug(Object message)
            {
                Log("DEBUG", message, null);
            }

            private void Log(String level, Object message, Exception exception)
            {
                _writer.Write(level);
                _writer.Write(" - ");
                _writer.WriteLine(message);
                if (exception != null)
                    _writer.WriteLine(exception);
            }

            public void Debug(Object message, Exception exception)
            {
                Log("DEBUG", message, exception);
            }

            public void Error(Object message)
            {
                Log("Error", message, null);
            }

            public void Error(Object message, Exception exception)
            {
                Log("Error", message, exception);
            }

            public void Fatal(Object message)
            {
                Log("Fatal", message, null);
            }

            public void Fatal(Object message, Exception exception)
            {
                Log("Fatal", message, exception);
            }

            public void Info(Object message)
            {
                Log("Info", message, null);
            }

            public void Info(Object message, Exception exception)
            {
                Log("Info", message, exception);
            }

            public void Warn(Object message)
            {
                Log("Warn", message, null);
            }

            public void Warn(Object message, Exception exception)
            {
                Log("Warn", message, exception);
            }
        }
    }
}
