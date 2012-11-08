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

namespace CoAP
{
    /// <summary>
    /// Constants defined for CoAP protocol
    /// </summary>
    public static class CoapConstants
    {
        /// <summary>
        /// The URI scheme for identifying CoAP resources
        /// </summary>
        public const String UriSchemeName = "coap";
        /// <summary>
        /// The default port for CoAP server
        /// </summary>
        public const Int32 DefaultPort = 5683;
        /// <summary>
        /// The initial time (ms) for a CoAP message
        /// </summary>
        public const Int32 ResponseTimeout = 2000;
        /// <summary>
        /// The initial timeout is set
        /// to a random number between RESPONSE_TIMEOUT and (RESPONSE_TIMEOUT *
        /// RESPONSE_RANDOM_FACTOR)
        /// </summary>
        public const Double ResponseRandomFactor = 1.5D;
        /// <summary>
        /// The max time that a message would be retransmitted
        /// </summary>
        public const Int32 MaxRetransmit = 4;
        /// <summary>
        /// Default block size used for block-wise transfers
        /// </summary>
        public const Int32 DefaultBlockSize = 512;
        public const Int32 MessageCacheSize = 32;
        public const Int32 ReceiveBufferSize = 4096;
        /// <summary>
        /// Default timeout (ms) of transactions
        /// </summary>
        public const Int32 DefaultTransactionTimeout = 100000;
        public const Int32 DefaultOverallTimeout = 60000;
        /// <summary>
        /// Default URI for wellknown resource
        /// </summary>
        public const String DefaultWellKnownURI = "/.well-known/core";
        public const Int32 TokenLength = 8;
        public const Int32 DefaultMaxAge = 60;
    }
}
