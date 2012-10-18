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

using System;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// Base class of layers that have a lower layer
    /// </summary>
    public abstract class UpperLayer : Layer
    {
        private Layer _lowerLayer;

        /// <summary>
        /// Gets or sets the lower layer of this layer.
        /// </summary>
        public Layer LowerLayer
        {
            get { return _lowerLayer; }
            set
            {
                if (null != _lowerLayer)
                {
                    _lowerLayer.UnregisterReceiver(this);
                }
                _lowerLayer = value;
                if (null != _lowerLayer)
                {
                    _lowerLayer.RegisterReceiver(this);
                }
            }
        }

        /// <summary>
        /// Sends a message by the lower layer.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected void SendMessageOverLowerLayer(Message msg)
        {
            if (null == _lowerLayer)
            {
                if (Log.IsWarningEnabled)
                    Log.Warning(this, "No lower layer present");
            }
            else
            {
                _lowerLayer.SendMessage(msg);
            }
        }
    }
}
