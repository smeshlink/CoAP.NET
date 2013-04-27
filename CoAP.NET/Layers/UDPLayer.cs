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
using System.Net;
using System.Net.Sockets;
using CoAP.Log;

namespace CoAP.Layers
{
    /// <summary>
    /// The class UDPLayer exchanges CoAP messages with remote endpoints using UDP
    /// datagrams. It is an unreliable channel and thus datagrams may arrive out of
    /// order, appear duplicated, or are lost without any notice, especially on lossy
    /// physical layers.
    /// </summary>
    public class UDPLayer : AbstractLayer
    {
        public const Int32 ReceiveBufferSize = 4096;
        private static ILogger log = LogManager.GetLogger(typeof(UDPLayer));

        private Int32 _port;
        private UDPSocket _socketV6;
        private UDPSocket _socketV4;
        private AsyncCallback _receiveCallback;
#if COAPALL
        public ISpec Spec { get; set; }
#endif

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
            _socketV6.Buffer = new Byte[ReceiveBufferSize + 1];  // +1 to check for > ReceiveBufferSize

            try
            {
                // Enable IPv4-mapped IPv6 addresses to accept both IPv6 and IPv4 connections in a same socket.
                _socketV6.Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
            }
            catch (Exception)
            {
                // IPv4-mapped address seems not to be supported, set up a separated socket of IPv4.
                _socketV4 = new UDPSocket();
                _socketV4.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socketV4.Buffer = new Byte[ReceiveBufferSize + 1];
            }

            _socketV6.Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            if (_socketV4 != null)
                _socketV4.Socket.Bind(new IPEndPoint(IPAddress.Any, port));

            _receiveCallback = new AsyncCallback(SocketReceiveCallback);
            BeginReceive();
        }

        public Int32 Port
        {
            get { return _port == 0 ? ((IPEndPoint)_socketV6.Socket.LocalEndPoint).Port : _port; }
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
            Byte[] data = Spec.Encode(msg);

            if (remoteEP.AddressFamily == AddressFamily.InterNetwork)
            {
                if (_socketV4 == null)
                {
                    // build IPv4 mapped address, i.e. ::ffff:127.0.0.1
                    Byte[] addrBytes = new Byte[16];
                    addrBytes[10] = addrBytes[11] = 0xFF;
                    Array.Copy(remoteEP.Address.GetAddressBytes(), 0, addrBytes, 12, 4);
                    IPAddress addr = new IPAddress(addrBytes);
                    _socketV6.Socket.SendTo(data, new IPEndPoint(addr, remoteEP.Port));
                }
                else
                {
                    // use the separated socket of IPv4 to deal with IPv4 conversions.
                    _socketV4.Socket.SendTo(data, remoteEP);
                }
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
            _socketV6.Socket.BeginReceiveFrom(_socketV6.Buffer, 0, _socketV6.Buffer.Length, SocketFlags.None,
                ref remoteV6, _receiveCallback, _socketV6);
            if (_socketV4 != null)
            {
                System.Net.EndPoint remoteV4 = new IPEndPoint(IPAddress.Any, 0);
                _socketV4.Socket.BeginReceiveFrom(_socketV4.Buffer, 0, _socketV4.Buffer.Length, SocketFlags.None,
                    ref remoteV4, _receiveCallback, _socketV4);
            }
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
                Message msg = Spec.Decode(data);

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
                    if (ipe.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        Byte[] addrBytes = ipe.Address.GetAddressBytes();
                        // if remote is a IPv4 mapped address, restore original address.
                        if (addrBytes[0] == 0x00 && addrBytes[1] == 0x00 && addrBytes[2] == 0x00
                            && addrBytes[3] == 0x00 && addrBytes[4] == 0x00 && addrBytes[5] == 0x00
                            && addrBytes[6] == 0x00 && addrBytes[7] == 0x00 && addrBytes[8] == 0x00
                            && addrBytes[9] == 0x00 && addrBytes[10] == 0xFF && addrBytes[11] == 0xFF)
                        {
                            IPAddress addrV4 = new IPAddress(new Byte[] {
                                addrBytes[12], addrBytes[13], addrBytes[14], addrBytes[15] });
                            ipe = new IPEndPoint(addrV4, ipe.Port);
                        }
                    }
                    msg.PeerAddress = new EndpointAddress(ipe.Address, ipe.Port);

                    if (data.Length > ReceiveBufferSize)
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

        class UDPSocket
        {
            public Socket Socket;
            public Byte[] Buffer;
        }
    }
}
