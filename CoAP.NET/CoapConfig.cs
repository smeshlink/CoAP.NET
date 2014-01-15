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

namespace CoAP
{
    /// <summary>
    /// Default implementation of <see cref="ICoapConfig"/>.
    /// </summary>
    public class CoapConfig : ICoapConfig
    {
        private Int32 _port;
        private Int32 _httpPort;
        private Int32 _transferBlockSize = CoapConstants.DefaultBlockSize;
        private Int32 _sequenceTimeout = CoapConstants.DefaultOverallTimeout;

        public Int32 Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public Int32 HttpPort
        {
            get { return _httpPort; }
            set { _httpPort = value; }
        }

        public Int32 TransferBlockSize
        {
            get { return _transferBlockSize; }
            set { _transferBlockSize = value; }
        }

        public Int32 SequenceTimeout
        {
            get { return _sequenceTimeout; }
            set { _sequenceTimeout = value; }
        }
        
#if COAPALL
        private ISpec _spec;

        public ISpec Spec
        {
            get { return _spec; }
            set { _spec = value; }
        }
#endif
    }
}
