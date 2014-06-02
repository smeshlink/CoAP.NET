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
    /// Provides methods to parse incoming byte arrays to messages.
    /// </summary>
    public interface IMessageDecoder
    {
        Boolean IsWellFormed { get; }
        Boolean IsReply { get; }
        Boolean IsRequest { get; }
        Boolean IsResponse { get; }
        Boolean IsEmpty { get; }
        Int32 Version { get; }
        Int32 ID { get; }
        Request ParseRequest();
        Response ParseResponse();
        EmptyMessage ParseEmptyMessage();
    }
}
