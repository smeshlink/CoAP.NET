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
using CoAP.Layers;

namespace CoAP
{
    /// <summary>
    /// Class for message communicating.
    /// </summary>
    public static class Communicator
    {
        private static CommonCommunicator instance;

        public static CommonCommunicator Default
        {
            get
            {
                if (instance == null)
                {
                    //lock (typeof(CommonCommunicator))
                    //{
                    //    if (instance == null)
                    //    {
                    //        instance = new CommonCommunicator(0, CoapConstants.DefaultBlockSize);
                    //    }
                    //}
                }
                return instance;
            }
        }

        public class CommonCommunicator : UpperLayer
        {
            private readonly CoapStack _coapStack;

            public CommonCommunicator(ISpec spec)
            {
                _coapStack = new CoapStack(spec);
                LowerLayer = _coapStack;
            }

            public CommonCommunicator(Int32 port, Int32 transferBlockSize)
            {
                _coapStack = new CoapStack(port, transferBlockSize);
                LowerLayer = _coapStack;
            }

            public Int32 Port
            {
                get { return _coapStack.Port; }
            }

            protected override void DoSendMessage(Message msg)
            {
                if (msg != null)
                {
                    if (msg.PeerAddress == null || msg.PeerAddress.Address == null)
                        throw new InvalidOperationException("Remote address not specified");
                    SendMessageOverLowerLayer(msg);
                }
            }

            protected override void DoReceiveMessage(Message msg)
            {
                msg.Communicator = this;
                if (msg is Response)
                {
                    Response response = (Response)msg;
                    if (response.Request != null)
                        response.Request.HandleResponse(response);
                }

                DeliverMessage(msg);
            }
        }
    }
}
