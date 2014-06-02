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
    /// Provides configuration for <see cref="ICommunicator"/>.
    /// </summary>
    public interface ICoapConfig
    {
        /// <summary>
        /// Gets the port which CoAP endpoint is on.
        /// </summary>
        Int32 Port { get; }
        /// <summary>
        /// Gets the port which HTTP proxy is on.
        /// </summary>
        Int32 HttpPort { get; }
        /// <summary>
        /// Gets the preferred size of block in blockwise transfer.
        /// </summary>
        Int32 TransferBlockSize { get; }
        /// <summary>
        /// Gets the overall timeout for CoAP request/response(s).
        /// </summary>
        Int32 SequenceTimeout { get; }
#if COAPALL
        /// <summary>
        /// Gets the specification to apply.
        /// </summary>
        ISpec Spec { get; }
#endif
        Boolean UseRandomIDStart { get; }
        Boolean UseRandomTokenStart { get; }
        Int32 MaxMessageSize { get; }
        Int32 DefaultBlockSize { get; }
        Int32 NotificationReregistrationBackoff { get; }

        String Deduplicator { get; }
        Int32 CropRotationPeriod { get; }
        Int32 ExchangeLifecycle { get; }
        Int32 MarkAndSweepInterval { get; }
    }
}
