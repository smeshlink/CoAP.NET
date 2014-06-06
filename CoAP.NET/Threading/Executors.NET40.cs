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

namespace CoAP.Threading
{
    public static partial class Executors
    {
        public static readonly IExecutor Default = new TaskExecutor();

        public static readonly IExecutor ThreadPool = new ThreadPoolExecutor();
    }
}
