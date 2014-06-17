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
using System.Net;
using System.Net.Sockets;

namespace CoAP.Channel
{
    public partial class UDPChannel
    {
        private UDPSocket NewUDPSocket(AddressFamily addressFamily, Int32 bufferSize)
        {
            return new UDPSocket(addressFamily, bufferSize);
        }

        private void BeginReceive(UDPSocket socket)
        {
            if (_running == 0)
                return;

            System.Net.EndPoint remoteEP = new IPEndPoint(
                    socket.Socket.AddressFamily == AddressFamily.InterNetwork ?
                    IPAddress.Any : IPAddress.IPv6Any, 0);

            try
            {
                socket.Socket.BeginReceiveFrom(socket.Buffer, 0, socket.Buffer.Length,
                    SocketFlags.None, ref remoteEP, ReceiveCallback, socket);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
            catch (Exception ex)
            {
                EndReceive(socket, ex);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            UDPSocket socket = (UDPSocket)ar.AsyncState;
            System.Net.EndPoint remoteEP = new IPEndPoint(
                socket.Socket.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any, 0);

            Int32 count = 0;
            try
            {
                count = socket.Socket.EndReceiveFrom(ar, ref remoteEP);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndReceive(socket, ex);
                return;
            }

            EndReceive(socket, socket.Buffer, 0, count, remoteEP);
        }

        private void BeginSend(UDPSocket socket, Byte[] data, System.Net.EndPoint destination)
        {
            try
            {
                socket.Socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, destination, SendCallback, socket);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
            catch (Exception ex)
            {
                EndSend(socket, ex);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            UDPSocket socket = (UDPSocket)ar.AsyncState;

            Int32 written;
            try
            {
                written = socket.Socket.EndSendTo(ar);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndSend(socket, ex);
                return;
            }

            EndSend(socket, written);
        }

        partial class UDPSocket
        {
            public readonly Byte[] Buffer;

            public UDPSocket(AddressFamily addressFamily, Int32 bufferSize)
            {
                Socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
                Buffer = new Byte[bufferSize];
            }

            public void Dispose()
            {
                Socket.Close();
            }
        }
    }
}
