/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;

namespace CoAP.Layers
{
    /// <summary>
    /// Base class of all layers in CoAP communication
    /// </summary>
    public abstract class Layer : IMessageReceiver
    {
        private List<IMessageReceiver> _receivers;
        private Int32 _messagesSentCount;
        private Int32 _messagesReceivedCount;

        /// <summary>
        /// Gets the total number of sent messages.
        /// </summary>
        public Int32 MessagesSentCount
        {
            get { return _messagesSentCount; }
        }

        /// <summary>
        /// Gets the total number of received messages.
        /// </summary>
        public Int32 MessagesReceivedCount
        {
            get { return _messagesReceivedCount; }
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveMessage(Message msg)
        {
            if (msg != null)
            {
                ++_messagesReceivedCount;
                DoReceiveMessage(msg);
            }
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="msg"></param>
        public void SendMessage(Message msg)
        {
            if (msg != null)
            {
                DoSendMessage(msg);
                ++_messagesSentCount;
            }
        }

        /// <summary>
        /// Adds a message receiver to this layer.
        /// </summary>
        /// <param name="receiver"></param>
        public void RegisterReceiver(IMessageReceiver receiver)
        {
            // check for valid receiver
            if (receiver != null && receiver != this)
            {
                if (_receivers == null)
                {
                    _receivers = new List<IMessageReceiver>();
                }
                _receivers.Add(receiver);
            }
        }

        /// <summary>
        /// Removes a message receiver from this layer.
        /// </summary>
        /// <param name="receiver"></param>
        public void UnregisterReceiver(IMessageReceiver receiver)
        {
            if (_receivers != null)
            {
                _receivers.Remove(receiver);
            }
        }

        /// <summary>
        /// Delivers a message to registered receivers.
        /// </summary>
        /// <param name="msg">The message to be delivered</param>
        protected void DeliverMessage(Message msg)
        {
            // pass message to registered receivers
            if (_receivers != null)
            {
                foreach (IMessageReceiver receiver in _receivers)
                {
                    receiver.ReceiveMessage(msg);
                }
            }
        }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected abstract void DoSendMessage(Message msg);
        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
	    protected abstract void DoReceiveMessage(Message msg);
    }
}
