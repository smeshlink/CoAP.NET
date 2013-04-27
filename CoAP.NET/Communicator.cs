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

#if COAPALL
        private static CommonCommunicator draft03;
        private static CommonCommunicator draft08;
        private static CommonCommunicator draft12;
        private static CommonCommunicator draft13;

        public static CommonCommunicator Draft03
        {
            get
            {
                if (draft03 == null)
                {
                    lock (typeof(Communicator))
                    {
                        if (draft03 == null)
                            draft03 = CreateCommunicator(0, 0, Spec.Draft03);
                    }
                }
                return draft03;
            }
        }

        public static CommonCommunicator Draft08
        {
            get
            {
                if (draft08 == null)
                {
                    lock (typeof(Communicator))
                    {
                        if (draft08 == null)
                            draft08 = CreateCommunicator(0, 0, Spec.Draft08);
                    }
                }
                return draft08;
            }
        }

        public static CommonCommunicator Draft12
        {
            get
            {
                if (draft12 == null)
                {
                    lock (typeof(Communicator))
                    {
                        if (draft12 == null)
                            draft12 = CreateCommunicator(0, 0, Spec.Draft12);
                    }
                }
                return draft12;
            }
        }

        public static CommonCommunicator Draft13
        {
            get
            {
                if (draft13 == null)
                {
                    lock (typeof(Communicator))
                    {
                        if (draft13 == null)
                            draft13 = CreateCommunicator(0, 0, Spec.Draft13);
                    }
                }
                return draft13;
            }
        }
#endif

        public static CommonCommunicator Default
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
                            instance = new CommonCommunicator(0, 0);
#endif
                        }
                    }
                }
                return instance;
            }
        }

        public static CommonCommunicator CreateCommunicator(Int32 port, Int32 transferBlockSize)
        {
            return new CommonCommunicator(port, transferBlockSize);
        }

#if COAPALL
        public static CommonCommunicator CreateCommunicator(Int32 port, Int32 transferBlockSize, ISpec spec)
        {
            return new CommonCommunicator(port, transferBlockSize, spec);
        }
#endif

        public class CommonCommunicator : UpperLayer, IShutdown
        {
            private readonly CoapStack _coapStack;

#if COAPALL
            public CommonCommunicator(Int32 port, Int32 transferBlockSize, ISpec spec)
            {
                _coapStack = new CoapStack(port, transferBlockSize, spec);
                LowerLayer = _coapStack;
            }
#endif

            public CommonCommunicator(Int32 port, Int32 transferBlockSize)
            {
                _coapStack = new CoapStack(port, transferBlockSize);
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
    }
}
