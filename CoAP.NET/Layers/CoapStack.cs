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

namespace CoAP.Layers
{
    /// <summary>
    /// The CoapStack encapsulates the layers needed to communicate to CoAP nodes.
    /// </summary>
    public class CoapStack : UpperLayer
    {
        public CoapStack(Int32 port, Int32 transferBlockSize)
        {
            TokenLayer tokenLayer = new TokenLayer();
            TransferLayer transferLayer = new TransferLayer(transferBlockSize);
            MatchingLayer matchingLayer = new MatchingLayer();
            MessageLayer messageLayer = new MessageLayer();
            UDPLayer udpLayer = new UDPLayer(port);

            this.LowerLayer = tokenLayer;
            tokenLayer.LowerLayer = transferLayer;
            transferLayer.LowerLayer = matchingLayer;
            matchingLayer.LowerLayer = messageLayer;
            messageLayer.LowerLayer = udpLayer;
        }
    }
}
