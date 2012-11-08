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

namespace CoAP
{
    public class EndpointAddress
    {
        private IPAddress _address = null;
        private Int32 _port = CoapConstants.DefaultPort;

        public EndpointAddress(IPAddress address)
        {
            _address = address;
        }

        public EndpointAddress(IPAddress address, Int32 port)
        {
            _address = address;
            _port = port;
        }

        public EndpointAddress(Uri uri)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(uri.Host);
            if (addresses.Length > 0)
                _address = addresses[0];
            if (uri.Port != -1)
                _port = uri.Port;
        }

        public IPAddress Address
        {
            get { return _address; }
        }

        public Int32 Port
        {
            get { return _port; }
        }

        public override String ToString()
        {
            if (_address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return String.Format("[{0}]:{1}", _address, _port);
            else
                return String.Format("{0}:{1}", _address, _port);
        }
    }
}
