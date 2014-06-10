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
    class ConsoleLogManager : ILogManager
    {
        static readonly ILogger _logger = new TextWriterLogger(Console.Out);

        public ILogger GetLogger(Type type)
        {
            return _logger;
        }

        public ILogger GetLogger(string name)
        {
            return _logger;
        }
    }
}
