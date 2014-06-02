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

namespace CoAP.Codec
{
    /// <summary>
    /// Provides methods to serialize outgoing messages to byte arrays.
    /// </summary>
    public interface IMessageEncoder
    {
        Byte[] Encode(Request request);
        Byte[] Encode(Response response);
        Byte[] Encode(EmptyMessage message);
    }
}
