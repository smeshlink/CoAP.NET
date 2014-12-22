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
using System.Collections.Specialized;
using System.IO;

namespace CoAP.Http
{
    public interface IHttpRequest
    {
        String Url { get; }
        String RequestUri { get; }
        String QueryString { get; }
        String Method { get; }
        NameValueCollection Headers { get; }
        Stream InputStream { get; }
        String Host { get; }
        String UserAgent { get; }
        String GetParameter(String name);
        String[] GetParameters(String name);
        Object this[Object key] { get; set; }
    }
}
