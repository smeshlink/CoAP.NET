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
        
        private Socket _socket;
        private AsyncCallback _receiveCallback;
        private Byte[] _buffer = new Byte[CoapConstants.ReceiveBufferSize + 1];  // +1 to check for > ReceiveBufferSize

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
            IPEndPoint localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            this._socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                // Enable IPv4-mapped to accept both ipv6 and ipv4 connections in a same socket.
                this._socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
            }
            catch (Exception ex)
            { 
                // ignore it
                if (log.IsWarnEnabled)
                    log.Warn("UDPLayer - " + ex.Message);
            }
            this._receiveCallback = new AsyncCallback(SocketReceiveCallback);
            this._socket.Bind(localEP);
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
            this._socket.SendTo(data, remoteEP);
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
            System.Net.EndPoint remote = new IPEndPoint(System.Net.IPAddress.IPv6Any, 0);
            this._socket.BeginReceiveFrom(this._buffer, 0, this._buffer.Length, SocketFlags.None, ref remote, this._receiveCallback, null);
        }

        private void SocketReceiveCallback(IAsyncResult ar)
        {
            System.Net.EndPoint remoteEP = new IPEndPoint(System.Net.IPAddress.IPv6Any, 0);
            Int32 count = 0;
            try
            {
                count = this._socket.EndReceiveFrom(ar, ref remoteEP);
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
                Buffer.BlockCopy(this._buffer, 0, bytes, 0, count);
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
    }
}
