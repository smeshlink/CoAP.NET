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
using System.IO;

namespace CoAP.Http
{
    public interface IHttpResponse
    {
        Stream OutputStream { get; }
        void AppendHeader(String name, String value);
        Int32 StatusCode { get; set; }
        String StatusDescription { get; set; }
    }
}
