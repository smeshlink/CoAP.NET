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
using System.Net;
using System.Net.Sockets;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// This class describes the functionality of a UDP layer that is able to exchange CoAP messages.
    /// </summary>
    public class UDPLayer : Layer
    {
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
            // TODO Enable IPv4-mapped to accept both ipv6 and ipv4 connections in a same socket.
            try
            {
                this._socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
            }
            catch (Exception ex)
            { 
                // ignore it
                if (Log.IsWarningEnabled)
                    Log.Warning(this, ex.Message);
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
            {
                msg.Timestamp = DateTime.Now.Ticks;
            }

            Int32 port = (null == msg.URI) ? -1 : msg.URI.Port;
            if (port < 0)
                port = CoapConstants.DefaultPort;
            IPEndPoint remoteEP = new IPEndPoint(msg.Address, port);
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
            catch (SocketException ex)
            {
                if (Log.IsErrorEnabled)
                    Log.Error(this, ex.Message);
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
            Int64 timestamp = DateTime.Now.Ticks;
            Message msg = Message.Decode(data);
            // remember when this message was received
            msg.Timestamp = timestamp;

            IPEndPoint ipe = (IPEndPoint)remoteEP;
            // TODO 检查IP地址是否需要[]
            msg.URI = new Uri(String.Format("{0}://[{1}]:{2}", CoapConstants.UriSchemeName, ipe.Address, ipe.Port));

            if (data.Length > CoapConstants.ReceiveBufferSize)
            {
                if (Log.IsInfoEnabled)
                    Log.Info(this, "Large datagram received, marking for blockwise transfer | {0}", msg.Key);
                msg.RequiresBlockwise = true;
            }

            ReceiveMessage(msg);
        }
    }
}
