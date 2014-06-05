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
        private static ICoapConfig _default;

        public static ICoapConfig Default
        {
            get
            {
                if (_default == null)
                {
                    lock (typeof(CoapConfig))
                    {
                        if (_default == null)
                            _default = LoadConfig();
                    }
                }
                return _default;
            }
        }

#if COAPALL
        private Int32 _port = CoapConstants.DefaultPort;
#else
        private Int32 _port = Spec.DefaultPort;
#endif
        private Int32 _securePort = CoapConstants.DefaultSecurePort;
        private Int32 _httpPort = 8080;
        private Int32 _ackTimeout = CoapConstants.AckTimeout;
        private Double _ackRandomFactor = CoapConstants.AckRandomFactor;
        private Int32 _ackTimeoutScale = 2;
        private Int32 _maxRetransmit = CoapConstants.MaxRetransmit;
        private Int32 _maxMessageSize = 1024;
        private Int32 _defaultBlockSize = CoapConstants.DefaultBlockSize;
        private Boolean _useRandomIDStart = true;
        private Boolean _useRandomTokenStart = true;
        private String _deduplicator = CoAP.Deduplication.DeduplicatorFactory.MarkAndSweepDeduplicator;
        private Int32 _cropRotationPeriod = 2000; // ms
        private Int32 _exchangeLifecycle = 247 * 1000; // ms
        private Int64 _markAndSweepInterval = 10 * 1000; // ms
        private Int64 _notificationMaxAge = 128 * 1000; // ms
        private Int64 _notificationCheckIntervalTime = 24 * 60 * 60 * 1000; // ms
        private Int32 _notificationCheckIntervalCount = 100; // ms
        private Int32 _notificationReregistrationBackoff = 2000; // ms
        private Int32 _channelReceiveBufferSize;
        private Int32 _channelSendBufferSize;
        private Int32 _channelReceivePacketSize = 2048;

#if COAPALL
        private ISpec _spec = CoAP.Spec.Draft13;

        public ISpec Spec
        {
            get { return _spec; }
            set { _spec = value; }
        }
#endif
        
        /// <inheritdoc/>
        public Int32 DefaultPort
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <inheritdoc/>
        public Int32 DefaultSecurePort
        {
            get { return _securePort; }
            set { _securePort = value; }
        }

        /// <inheritdoc/>
        public Int32 HttpPort
        {
            get { return _httpPort; }
            set { _httpPort = value; }
        }

        /// <inheritdoc/>
        public Int32 AckTimeout
        {
            get { return _ackTimeout; }
            set { _ackTimeout = value; }
        }

        /// <inheritdoc/>
        public Double AckRandomFactor
        {
            get { return _ackRandomFactor; }
            set { _ackRandomFactor = value; }
        }

        /// <inheritdoc/>
        public Int32 AckTimeoutScale
        {
            get { return _ackTimeoutScale; }
            set { _ackTimeoutScale = value; }
        }

        /// <inheritdoc/>
        public Int32 MaxRetransmit
        {
            get { return _maxRetransmit; }
            set { _maxRetransmit = value; }
        }

        /// <inheritdoc/>
        public Int32 MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = value; }
        }

        /// <inheritdoc/>
        public Int32 DefaultBlockSize
        {
            get { return _defaultBlockSize; }
            set { _defaultBlockSize = value; }
        }

        /// <inheritdoc/>
        public Boolean UseRandomIDStart
        {
            get { return _useRandomIDStart; }
            set { _useRandomIDStart = value; }
        }

        /// <inheritdoc/>
        public Boolean UseRandomTokenStart
        {
            get { return _useRandomTokenStart; }
            set { _useRandomTokenStart = value; }
        }

        /// <inheritdoc/>
        public String Deduplicator
        {
            get { return _deduplicator; }
            set { _deduplicator = value; }
        }

        /// <inheritdoc/>
        public Int32 CropRotationPeriod
        {
            get { return _cropRotationPeriod; }
            set { _cropRotationPeriod = value; }
        }

        /// <inheritdoc/>
        public Int32 ExchangeLifecycle
        {
            get { return _exchangeLifecycle; }
            set { _exchangeLifecycle = value; }
        }

        /// <inheritdoc/>
        public Int64 MarkAndSweepInterval
        {
            get { return _markAndSweepInterval; }
            set { _markAndSweepInterval = value; }
        }

        /// <inheritdoc/>
        public Int64 NotificationMaxAge
        {
            get { return _notificationMaxAge; }
            set { _notificationMaxAge = value; }
        }

        /// <inheritdoc/>
        public Int64 NotificationCheckIntervalTime
        {
            get { return _notificationCheckIntervalTime; }
            set { _notificationCheckIntervalTime = value; }
        }

        /// <inheritdoc/>
        public Int32 NotificationCheckIntervalCount
        {
            get { return _notificationCheckIntervalCount; }
            set { _notificationCheckIntervalCount = value; }
        }

        /// <inheritdoc/>
        public Int32 NotificationReregistrationBackoff
        {
            get { return _notificationReregistrationBackoff; }
            set { _notificationReregistrationBackoff = value; }
        }

        /// <inheritdoc/>
        public Int32 ChannelReceiveBufferSize
        {
            get { return _channelReceiveBufferSize; }
            set { _channelReceiveBufferSize = value; }
        }

        /// <inheritdoc/>
        public Int32 ChannelSendBufferSize
        {
            get { return _channelSendBufferSize; }
            set { _channelSendBufferSize = value; }
        }

        /// <inheritdoc/>
        public Int32 ChannelReceivePacketSize
        {
            get { return _channelReceivePacketSize; }
            set { _channelReceivePacketSize = value; }
        }

        private static ICoapConfig LoadConfig()
        {
            // TODO may have configuration file here
            return new CoapConfig();
        }
    }
}
