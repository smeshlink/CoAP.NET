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

using System;
using CoAP.Log;

namespace CoAP.Layers
{
    /// <summary>
    /// This class describes the functionality of a layer that drops messages
    /// with a given probability in order to test retransmissions between
    /// MessageLayer and UDPLayer etc.
    /// </summary>
    public class AdverseLayer : UpperLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(AdverseLayer));

        private Double _sendPacketLossProbability;
        private Double _receivePacketLossProbability;
        private Random _rand = new Random();

        public AdverseLayer()
            : this(0.01D, 0D)
        { }

        public AdverseLayer(Double sendPacketLossProbability, Double receivePacketLossProbability)
        {
            _sendPacketLossProbability = sendPacketLossProbability;
            _receivePacketLossProbability = receivePacketLossProbability;
        }

        protected override void DoSendMessage(Message msg)
        {
            if (_rand.NextDouble() >= _sendPacketLossProbability)
                base.DoSendMessage(msg);
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn(String.Format("AdverseLayer -  Outgoing message dropped: {0}", msg.Key));
            }
        }

        protected override void DoReceiveMessage(Message msg)
        {
            if (_rand.NextDouble() >= _receivePacketLossProbability)
                base.DoReceiveMessage(msg);
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn(String.Format("AdverseLayer -  Incoming message dropped: {0}", msg.Key));
            }
        }
    }
}
