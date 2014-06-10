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
    /// Logger that writes logs to a <see cref="System.IO.TextWriter"/>.
    /// </summary>
    public class TextWriterLogger : ILogger
    {
        private System.IO.TextWriter _writer;

        public TextWriterLogger(System.IO.TextWriter writer)
        {
            _writer = writer;
        }

        /// <inheritdoc/>
        public Boolean IsDebugEnabled
        {
            get { return LogLevel.Debug >= LogManager.Level; }
        }

        /// <inheritdoc/>
        public Boolean IsInfoEnabled
        {
            get { return LogLevel.Info >= LogManager.Level; }
        }

        /// <inheritdoc/>
        public Boolean IsErrorEnabled
        {
            get { return LogLevel.Error >= LogManager.Level; }
        }

        /// <inheritdoc/>
        public Boolean IsFatalEnabled
        {
            get { return LogLevel.Fatal >= LogManager.Level; }
        }

        /// <inheritdoc/>
        public Boolean IsWarnEnabled
        {
            get { return LogLevel.Warning >= LogManager.Level; }
        }

        /// <inheritdoc/>
        public void Error(Object sender, String msg, params Object[] args)
        {
            String format = String.Format("ERROR - {0}\n", msg);
            if (sender != null)
            {
                format = "[" + sender.GetType().Name + "] " + format;
            }

            _writer.Write(format, args);
        }

        /// <inheritdoc/>
        public void Warning(Object sender, String msg, params Object[] args)
        {
            String format = String.Format("WARNING - {0}\n", msg);
            if (sender != null)
            {
                format = "[" + sender.GetType().Name + "] " + format;
            }

            _writer.Write(format, args);
        }

        /// <inheritdoc/>
        public void Info(Object sender, String msg, params Object[] args)
        {
            String format = String.Format("INFO - {0}\n", msg);
            if (sender != null)
            {
                format = "[" + sender.GetType().Name + "] " + format;
            }

            _writer.Write(format, args);
        }

        /// <inheritdoc/>
        public void Debug(Object sender, String msg, params Object[] args)
        {
            String format = String.Format("DEBUG - {0}\n", msg);
            if (sender != null)
            {
                format = "[" + sender.GetType().Name + "] " + format;
            }

            _writer.Write(format, args);
        }

        /// <inheritdoc/>
        public void Debug(Object message)
        {
            Log("DEBUG", message, null);
        }

        /// <inheritdoc/>
        public void Debug(Object message, Exception exception)
        {
            Log("DEBUG", message, exception);
        }

        /// <inheritdoc/>
        public void Error(Object message)
        {
            Log("Error", message, null);
        }

        /// <inheritdoc/>
        public void Error(Object message, Exception exception)
        {
            Log("Error", message, exception);
        }

        /// <inheritdoc/>
        public void Fatal(Object message)
        {
            Log("Fatal", message, null);
        }

        /// <inheritdoc/>
        public void Fatal(Object message, Exception exception)
        {
            Log("Fatal", message, exception);
        }

        /// <inheritdoc/>
        public void Info(Object message)
        {
            Log("Info", message, null);
        }

        /// <inheritdoc/>
        public void Info(Object message, Exception exception)
        {
            Log("Info", message, exception);
        }

        /// <inheritdoc/>
        public void Warn(Object message)
        {
            Log("Warn", message, null);
        }

        /// <inheritdoc/>
        public void Warn(Object message, Exception exception)
        {
            Log("Warn", message, exception);
        }

        private void Log(String level, Object message, Exception exception)
        {
            _writer.Write(level);
            _writer.Write(" - ");
            _writer.WriteLine(message);
            if (exception != null)
                _writer.WriteLine(exception);
        }
    }
}
