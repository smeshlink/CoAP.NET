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
using System.IO;
using CoAP.Layers;

namespace CoAP
{
    /// <summary>
    /// Provides methods for message communication.
    /// </summary>
    public interface ICommunicator : ILayer
    {
        /// <summary>
        /// Gets the port which this communicator is on.
        /// </summary>
        Int32 Port { get; }
    }

    /// <summary>
    /// Factory for creating Communicator objects.
    /// </summary>
    public static class CommunicatorFactory
    {
        private static ICommunicator instance;

#if COAPALL
        private static ICommunicator draft03;
        private static ICommunicator draft08;
        private static ICommunicator draft12;
        private static ICommunicator draft13;

        public static ICommunicator Draft03
        {
            get
            {
                if (draft03 == null)
                {
                    lock (typeof(CommunicatorFactory))
                    {
                        if (draft03 == null)
                            draft03 = CreateCommunicator(0, 0, Spec.Draft03);
                    }
                }
                return draft03;
            }
        }

        public static ICommunicator Draft08
        {
            get
            {
                if (draft08 == null)
                {
                    lock (typeof(CommunicatorFactory))
                    {
                        if (draft08 == null)
                            draft08 = CreateCommunicator(0, 0, Spec.Draft08);
                    }
                }
                return draft08;
            }
        }

        public static ICommunicator Draft12
        {
            get
            {
                if (draft12 == null)
                {
                    lock (typeof(CommunicatorFactory))
                    {
                        if (draft12 == null)
                            draft12 = CreateCommunicator(0, 0, Spec.Draft12);
                    }
                }
                return draft12;
            }
        }

        public static ICommunicator Draft13
        {
            get
            {
                if (draft13 == null)
                {
                    lock (typeof(CommunicatorFactory))
                    {
                        if (draft13 == null)
                            draft13 = CreateCommunicator(0, 0, Spec.Draft13);
                    }
                }
                return draft13;
            }
        }
#endif

        public static ICommunicator Default
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(CommonCommunicator))
                    {
                        if (instance == null)
                        {
#if COAPALL
                            instance = Draft13;
#else
                            instance = CreateCommunicator(0, 0);
#endif
                        }
                    }
                }
                return instance;
            }
        }

        public static ICommunicator CreateCommunicator(Int32 port, Int32 transferBlockSize)
        {
            CoapConfig config = new CoapConfig();
            config.DefaultPort = port;
            config.DefaultBlockSize = transferBlockSize;
            return CreateCommunicator(config);
        }

        public static ICommunicator CreateCommunicator(Int32 port, Int32 httpPort, Int32 transferBlockSize)
        {
            CoapConfig config = new CoapConfig();
            config.DefaultPort = port;
            config.HttpPort = httpPort;
            config.DefaultBlockSize = transferBlockSize;
            return CreateCommunicator(config);
        }

        public static ICommunicator CreateCommunicator(ICoapConfig config)
        {
            if (config.HttpPort > 0)
                return new ProxyCommunicator(config);
            else
                return new CommonCommunicator(config);
        }

#if COAPALL
        public static ICommunicator CreateCommunicator(Int32 port, Int32 transferBlockSize, ISpec spec)
        {
            CoapConfig config = new CoapConfig();
            config.DefaultPort = port;
            config.DefaultBlockSize = transferBlockSize;
            config.Spec = spec;
            return CreateCommunicator(config);
        }
#endif

        public class CommonCommunicator : UpperLayer, ICommunicator, IShutdown
        {
            private readonly CoapStack _coapStack;

            public CommonCommunicator(ICoapConfig config)
            {
                _coapStack = new CoapStack(config);
                LowerLayer = _coapStack;
            }

            public Int32 Port
            {
                get { return _coapStack.Port; }
            }

            public void Shutdown()
            {
                _coapStack.Shutdown();
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

        public class ProxyCommunicator : UpperLayer, ICommunicator, IShutdown
        {
            private readonly CoapStack _coapStack;
            private readonly HttpStack _httpStack;

            public ProxyCommunicator(ICoapConfig config)
            {
                _coapStack = new CoapStack(config);
                _httpStack = new HttpStack(config.HttpPort);

                _coapStack.RegisterReceiver(this);
                _httpStack.RegisterReceiver(this);
            }

            public Int32 Port
            {
                get { return _coapStack.Port; }
            }

            public void Shutdown()
            {
                _coapStack.Shutdown();
                _httpStack.Shutdown();
            }

            protected override void DoReceiveMessage(Message msg)
            {
                msg.Communicator = this;
                Response response = msg as Response;
                if (response != null)
                {
                    // initiate custom response handling
                    if (response.Request != null)
                        response.Request.HandleResponse(response);
                }

                base.DoReceiveMessage(msg);
            }

            protected override void DoSendMessage(Message msg)
            {
                // defensive programming before entering the stack, lower layers
			    // should assume a correct message.
                if (msg != null)
                {
                    // check message before sending through the stack
                    if (msg.PeerAddress.Address == null)
                        throw new IOException("Remote address not specified");

                    // the ProxyCommunicator can't use the API
                    // SendMessageOverLowerLayer because it has two lower layers

                    Response response = msg as Response;
                    if (response != null)
                    {
                        Request request = response.Request;

                        if (_httpStack.IsWaitingRequest(request))
                        {
                            if (msg.IsEmptyACK)
                                // if the message is not the actual response, but
                                // only an acknowledge, should not be forwarded
                                // (HTTP is on TCP so there is no need for acks in
                                // the application layer)
                                return;

                            _httpStack.SendMessage(msg);
                            return;
                        }
                    }

                    _coapStack.SendMessage(msg);
                }
            }
        }
    }
}
