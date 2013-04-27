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

namespace CoAP
{
    /// <summary>
    /// Stores IP address and port.
    /// </summary>
    public class EndpointAddress
    {
        private IPAddress _address = null;
        private Int32 _port = CoapConstants.DefaultPort;

        /// <summary>
        /// Instantiates an endpoint address using an IP address and the default port.
        /// </summary>
        public EndpointAddress(IPAddress address)
        {
            _address = address;
        }

        /// <summary>
        /// Instantiates an endpoint address with both IP and port.
        /// </summary>
        public EndpointAddress(IPAddress address, Int32 port)
        {
            _address = address;
            _port = port;
        }

        /// <summary>
        /// Instantiates an endpoint address with a Uri object.
        /// </summary>
        /// <param name="uri"></param>
        public EndpointAddress(Uri uri)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(uri.Host);
            if (addresses.Length > 0)
                _address = addresses[0];
            if (uri.Port != -1)
                _port = uri.Port;
        }

        /// <summary>
        /// Gets the IP address.
        /// </summary>
        public IPAddress Address
        {
            get { return _address; }
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public Int32 Port
        {
            get { return _port; }
        }

        /// <summary>
        /// To string.
        /// </summary>
        public override String ToString()
        {
            if (_address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return String.Format("[{0}]:{1}", _address, _port);
            else
                return String.Format("{0}:{1}", _address, _port);
        }

        /// <summary>
        /// Equals.
        /// </summary>
        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            if (this.GetType() != obj.GetType())
                return false;
            EndpointAddress other = (EndpointAddress)obj;
            if (_port != other._port)
                return false;
            if (_address == null)
            {
                if (other._address != null)
                    return false;
            }
            else if (!_address.Equals(other._address))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets hash code.
        /// </summary>
        public override Int32 GetHashCode()
        {
            Int32 prime = 31;
            Int32 result = 1;
            result = prime * result + ((_address == null) ? 0 : _address.GetHashCode());
            result = prime * result + _port;
            return result;
        }
    }
}
