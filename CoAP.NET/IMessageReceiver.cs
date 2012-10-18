/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

namespace CoAP
{
    /// <summary>
    /// Interface of message receivers
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="msg"></param>
        void ReceiveMessage(Message msg);
    }
}
