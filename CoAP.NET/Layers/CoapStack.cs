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

namespace CoAP.Layers
{
    /// <summary>
    /// The CoapStack encapsulates the layers needed to communicate to CoAP nodes.
    /// </summary>
    public class CoapStack : UpperLayer, IShutdown
    {
        private UDPLayer _udpLayer;

        public CoapStack(Int32 port, Int32 transferBlockSize)
            : this(port, transferBlockSize, CoapConstants.DefaultOverallTimeout)
        { }

        public CoapStack(Int32 port, Int32 transferBlockSize, Int32 sequenceTimeout)
        {
            TokenLayer tokenLayer = new TokenLayer(sequenceTimeout);
            TransferLayer transferLayer = new TransferLayer(transferBlockSize);
            MatchingLayer matchingLayer = new MatchingLayer();
            MessageLayer messageLayer = new MessageLayer();
            _udpLayer = new UDPLayer(port);

            this.LowerLayer = tokenLayer;
            tokenLayer.LowerLayer = transferLayer;
            transferLayer.LowerLayer = matchingLayer;
            matchingLayer.LowerLayer = messageLayer;
            messageLayer.LowerLayer = _udpLayer;
        }

        public CoapStack(ICoapConfig config)
            : this(config.DefaultPort, config.DefaultBlockSize, config.SequenceTimeout)
        {
#if COAPALL
            if (config.Spec != null)
                _udpLayer.Spec = config.Spec;
#endif
        }

        public Int32 Port
        {
            get { return _udpLayer.Port; }
        }

        public void Shutdown()
        {
            _udpLayer.Shutdown();
        }
    }
}
