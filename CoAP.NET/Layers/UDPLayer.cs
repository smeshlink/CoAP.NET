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
using System.Net;
using System.Net.Sockets;
using CoAP.Log;

namespace CoAP.Layers
{
    /// <summary>
    /// This class describes the functionality of a UDP layer that is able to exchange CoAP messages.
    /// </summary>
    public class UDPLayer : Layer
    {
        private static ILogger log = LogManager.GetLogger(typeof(UDPLayer));

        private Int32 _port;
        private UDPSocket _socketV6;
        private UDPSocket _socketV4;
        private AsyncCallback _receiveCallback;

        /// <summary>
        /// Initializes a UDP layer.
        /// </summary>
        public UDPLayer() 
            : this(0)
        { }

        /// <summary>
        /// Initializes a UDP layer with a certain port.
        /// </summary>
        /// <param name="port">The port which this UDP layer will bind to</param>
        public UDPLayer(Int32 port)
        {
            _port = port;

            _socketV6 = new UDPSocket();
            _socketV6.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            _socketV6.Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            _socketV6.Buffer = new Byte[CoapConstants.ReceiveBufferSize + 1];  // +1 to check for > ReceiveBufferSize

            _socketV4 = new UDPSocket();
            _socketV4.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socketV4.Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _socketV4.Buffer = new Byte[CoapConstants.ReceiveBufferSize + 1];

            _receiveCallback = new AsyncCallback(SocketReceiveCallback);
            BeginReceive();
        }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            // remember when this message was sent for the first time
            // set timestamp only once in order
            // to handle retransmissions correctly
            if (msg.Timestamp == 0)
                msg.Timestamp = DateTime.Now.Ticks;

            IPEndPoint remoteEP = new IPEndPoint(msg.PeerAddress.Address, msg.PeerAddress.Port);
            Byte[] data = msg.Encode();

            if (remoteEP.AddressFamily == AddressFamily.InterNetwork)
            {
                _socketV4.Socket.SendTo(data, remoteEP);
            }
            else
            {
                _socketV6.Socket.SendTo(data, remoteEP);
            }
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            // pass message to registered receivers
            DeliverMessage(msg);
        }

        private void BeginReceive()
        {
            System.Net.EndPoint remoteV6 = new IPEndPoint(IPAddress.IPv6Any, 0);
            System.Net.EndPoint remoteV4 = new IPEndPoint(IPAddress.Any, 0);
            _socketV6.Socket.BeginReceiveFrom(_socketV6.Buffer, 0, _socketV6.Buffer.Length, SocketFlags.None,
                ref remoteV6, _receiveCallback, _socketV6);
            _socketV4.Socket.BeginReceiveFrom(_socketV4.Buffer, 0, _socketV4.Buffer.Length, SocketFlags.None,
                ref remoteV4, _receiveCallback, _socketV4);
        }

        private void SocketReceiveCallback(IAsyncResult ar)
        {
            UDPSocket socket = (UDPSocket)ar.AsyncState;
            System.Net.EndPoint remoteEP = new IPEndPoint(
                socket.Socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
            Int32 count = 0;
            try
            {
                count = socket.Socket.EndReceiveFrom(ar, ref remoteEP);
            }
            catch (SocketException)
            {
                // ignore it
                //if (log.IsFatalEnabled)
                //    log.Fatal("UDPLayer - Failed receive datagram", ex);
            }
            if (count > 0)
            {
                Byte[] bytes = new Byte[count];
                Buffer.BlockCopy(socket.Buffer, 0, bytes, 0, count);
                Handle(bytes, remoteEP);
            }
            BeginReceive();
        }

        private void Handle(Byte[] data, System.Net.EndPoint remoteEP)
        {
            if (data.Length > 0)
            {
                Message msg = Message.Decode(data);

                if (msg == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error("UDPLayer - Illeagal datagram received: " + BitConverter.ToString(data));
                }
                else
                {
                    // remember when this message was received
                    msg.Timestamp = DateTime.Now.Ticks;
                    
                    IPEndPoint ipe = (IPEndPoint)remoteEP;
                    msg.PeerAddress = new EndpointAddress(ipe.Address, ipe.Port);

                    if (data.Length > CoapConstants.ReceiveBufferSize)
                    {
                        if (log.IsInfoEnabled)
                            log.Info(String.Format("UDPLayer - Marking large datagram for blockwise transfer: {0}", msg.Key));
                        msg.RequiresBlockwise = true;
                    }

                    try
                    {
                        ReceiveMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("UDPLayer - Crash: " + ex.Message, ex);
                    }
                }
            }
            else
            {
                if (log.IsDebugEnabled)
                    log.Debug(String.Format("UDPLayer - Dropped empty datagram from: {0}", remoteEP));
            }
        }

        public Int32 Port
        {
            get { return _port == 0 ? ((IPEndPoint)_socketV6.Socket.LocalEndPoint).Port : _port; }
        }

        class UDPSocket
        {
            public Socket Socket;
            public Byte[] Buffer;
        }
    }
}
