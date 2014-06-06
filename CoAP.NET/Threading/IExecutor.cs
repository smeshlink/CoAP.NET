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

namespace CoAP.Threading
{
    public interface IExecutor
    {
        void Start(Action task);
        void Start(Action<Object> task, Object obj);
    }

    public static partial class Executors
    {
        public static readonly IExecutor NoThreading = new NoThreadingExecutor();
    }
}
