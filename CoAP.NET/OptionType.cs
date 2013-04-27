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

namespace CoAP
{
    /// <summary>
    /// CoAP option types
    /// </summary>
    public enum OptionType
    {
        Unknown = -1,

        /// <summary>
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        Reserved = 0,
        /// <summary>
        /// C, opaque, 0-8 B, -
        /// <remarks>draft-ietf-core-coap-07</remarks>
        /// </summary>
        IfMatch = 1,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriHost = 3,
        /// <summary>
        /// E, sequence of bytes, 1-4 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        ETag = 4,
        /// <summary>
        /// <remarks>draft-ietf-core-coap-07</remarks>
        /// </summary>
        IfNoneMatch = 5,
        /// <summary>
        /// C, uint, 0-2 B
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriPort = 7,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        LocationPath = 8,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriPath = 11,
        /// <summary>
        /// C, 8-bit uint, 1 B, 0 (text/plain)
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        ContentType = 12,
        /// <summary>
        /// E, variable length, 1--4 B, 60 Seconds
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        MaxAge = 14,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriQuery = 15,
        /// <summary>
        /// E  Sequence of Bytes, 1-n B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        Accept = 16,
        /// <summary>
        /// C, Sequence of Bytes, 1-2 B, -
        /// <remarks>draft-ietf-core-coap-03, draft-ietf-core-coap-12</remarks>
        /// </summary>
        Token = 19,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        LocationQuery = 20,
        /// <summary>
        /// C, String, 1-270 B, "coap"
        /// <remarks>draft-ietf-core-coap-04</remarks>
        /// </summary>
        ProxyUri = 35,
        /// <summary>
        /// <remarks>draft-ietf-core-coap-13</remarks>
        /// </summary>
        ProxyScheme = 39,

        /// <summary>
        /// E, Duration, 1 B, 0
        /// <remarks>draft-ietf-core-observe</remarks>
        /// </summary>
        Observe = 6,

        /// <summary>
        /// <remarks>draft-ietf-core-block</remarks>
        /// </summary>
        Block2 = 23,
        /// <summary>
        /// <remarks>draft-ietf-core-block</remarks>
        /// </summary>
        Block1 = 27,
        /// <summary>
        /// <remarks>draft-ietf-core-block-08</remarks>
        /// </summary>
        Size = 28,

        /// <summary>
        /// no-op for fenceposting
        /// <remarks>draft-bormann-coap-misc-04</remarks>
        /// </summary>
        FencepostDivisor = 114,
    }
}
