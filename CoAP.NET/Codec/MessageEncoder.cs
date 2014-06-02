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
    public abstract class MessageEncoder : IMessageEncoder
    {
        /// <inheritdoc/>
        public Byte[] Serialize(Request request)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, request, request.Code);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public Byte[] Serialize(Response response)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, response, response.Code);
            return writer.ToByteArray();
        }

        /// <inheritdoc/>
        public Byte[] Serialize(EmptyMessage message)
        {
            DatagramWriter writer = new DatagramWriter();
            Serialize(writer, message, Code.Empty);
            return writer.ToByteArray();
        }

        protected abstract Byte[] Serialize(DatagramWriter writer, Message message, Int32 code);
    }
}
