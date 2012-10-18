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
using CoAP.Layers;

namespace CoAP
{
    /// <summary>
    /// Class for message communicating
    /// </summary>
    public class Communicator : UpperLayer
    {
        protected TransferLayer _transferLayer;
        protected TransactionLayer _transactionLayer;
        protected MessageLayer _messageLayer;
        //protected AdverseLayer adverseLayer;
        protected UDPLayer _udpLayer;

        protected TokenManager _tokenManager;

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        public Communicator()
            : this(0)
        { }

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        /// <param name="port">The local UDP port to listen for incoming messages</param>
        public Communicator(Int32 port)
            : this(port, CoapConstants.DefaultBlockSize)
        { }

        /// <summary>
        /// Initialize a communicator.
        /// </summary>
        /// <param name="port">The local UDP port to listen for incoming messages</param>
        /// <param name="defaultBlockSize">The default block size used for block-wise transfers, or -1 to disable outgoing block-wise transfers</param>
        public Communicator(Int32 port, Int32 defaultBlockSize)
        {
            _tokenManager = new TokenManager();

            _transferLayer = new TransferLayer(_tokenManager, defaultBlockSize);
            _transactionLayer = new TransactionLayer(_tokenManager);
            _messageLayer = new MessageLayer();
            _udpLayer = new UDPLayer(port);

            BuildStack();
        }

        protected override void DoSendMessage(Message msg)
        {
            SendMessageOverLowerLayer(msg);
        }

        protected override void DoReceiveMessage(Message msg)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;
                response.Handle();
            }
            else if (msg is Request)
            {
                Request request = (Request)msg;
                request.Communicator = this;
            }
            DeliverMessage(msg);
        }

        private void BuildStack()
        {
            //this.LowerLayer = _transferLayer;
            //_transferLayer.LowerLayer = _transactionLayer;
            //_transactionLayer.LowerLayer = _messageLayer;
            this.LowerLayer = _transactionLayer;
            _transactionLayer.LowerLayer = _transferLayer;
            _transferLayer.LowerLayer = _messageLayer;
            _messageLayer.LowerLayer = _udpLayer;
        }
    }
}
