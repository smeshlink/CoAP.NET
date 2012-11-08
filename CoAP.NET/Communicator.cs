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
using CoAP.Layers;

namespace CoAP
{
    /// <summary>
    /// Class for message communicating
    /// </summary>
    public class Communicator : UpperLayer
    {
        protected TokenLayer _tokenLayer;
        protected TransferLayer _transferLayer;
        protected MatchingLayer _matchingLayer;
        protected MessageLayer _messageLayer;
        //protected AdverseLayer adverseLayer;
        protected UDPLayer _udpLayer;

        private static Communicator instance;

        public static Communicator Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(Communicator))
                    {
                        if (instance == null)
                        {
                            instance = new Communicator();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        private Communicator()
            : this(0)
        { }

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        /// <param name="port">The local UDP port to listen for incoming messages</param>
        private Communicator(Int32 port)
            : this(port, CoapConstants.DefaultBlockSize)
        { }

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        /// <param name="port">The local UDP port to listen for incoming messages</param>
        /// <param name="defaultBlockSize">The default block size used for block-wise transfers, or -1 to disable outgoing block-wise transfers</param>
        public Communicator(Int32 port, Int32 defaultBlockSize)
        {
            _tokenLayer = new TokenLayer();
            _transferLayer = new TransferLayer(defaultBlockSize);
            _matchingLayer = new MatchingLayer();
            _messageLayer = new MessageLayer();
            _udpLayer = new UDPLayer(port);

            BuildStack();
        }

        protected override void DoSendMessage(Message msg)
        {
            if (msg != null)
            {
                if (msg.PeerAddress == null)
                    throw new InvalidOperationException("Remote address not specified");
                SendMessageOverLowerLayer(msg);
            }
        }

        protected override void DoReceiveMessage(Message msg)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;
                if (response.Request != null)
                    response.Request.HandleResponse(response);
            }
            
            DeliverMessage(msg);
        }

        private void BuildStack()
        {
            this.LowerLayer = _tokenLayer;
            _tokenLayer.LowerLayer = _transferLayer;
            _transferLayer.LowerLayer = _matchingLayer;
            _matchingLayer.LowerLayer = _messageLayer;
            _messageLayer.LowerLayer = _udpLayer;
        }
    }
}
