/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using CoAP.Log;

namespace CoAP.Layers
{
    /// <summary>
    /// Base class of layers that have a lower layer
    /// </summary>
    public abstract class UpperLayer : AbstractLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(UpperLayer));
        private ILayer _lowerLayer;

        /// <summary>
        /// Gets or sets the lower layer of this layer.
        /// </summary>
        public ILayer LowerLayer
        {
            get { return _lowerLayer; }
            set
            {
                if (null != _lowerLayer)
                    _lowerLayer.UnregisterReceiver(this);
                _lowerLayer = value;
                if (null != _lowerLayer)
                    _lowerLayer.RegisterReceiver(this);
            }
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            DeliverMessage(msg);
        }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            SendMessageOverLowerLayer(msg);
        }

        /// <summary>
        /// Sends a message by the lower layer.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected void SendMessageOverLowerLayer(Message msg)
        {
            if (null == _lowerLayer)
            {
                if (log.IsErrorEnabled)
                    log.Error(this.GetType().Name + ": No lower layer present");
            }
            else
            {
                _lowerLayer.SendMessage(msg);
            }
        }
    }
}
