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
        /// C, 8-bit uint, 1 B, 0 (text/plain)
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        ContentType = 1,
        /// <summary>
        /// E, variable length, 1--4 B, 60 Seconds
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        MaxAge = 2,
        /// <summary>
        /// C, String, 1-270 B, "coap"
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        ProxyUri = 3,
        /// <summary>
        /// E, sequence of bytes, 1-4 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        ETag = 4,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriHost = 5,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        LocationPath = 6,
        /// <summary>
        /// C, uint, 0-2 B
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriPort = 7,
        /// <summary>
        /// E, String, 1-270 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        LocationQuery = 8,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriPath = 9,
        /// <summary>
        /// C, Sequence of Bytes, 1-2 B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        Token = 11,
        /// <summary>
        /// C, String, 1-270 B, ""
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        UriQuery = 15,
        /// <summary>
        /// E  Sequence of Bytes, 1-n B, -
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        Accept = 12,
        /// <summary>
        /// C, unsigned integer, 1--3 B, 0
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        IfMatch = 13,
        /// <summary>
        /// <remarks>draft-ietf-core-coap</remarks>
        /// </summary>
        IfNoneMatch = 21,
        /// <summary>
        /// no-op for fenceposting
        /// <remarks>draft-bormann-coap-misc-04</remarks>
        /// </summary>
        FencepostDivisor = 14,

        /// <summary>
        /// E, Duration, 1 B, 0
        /// <remarks>draft-ietf-core-observe</remarks>
        /// </summary>
        Observe = 10,

        /// <summary>
        /// <remarks>draft-ietf-core-block</remarks>
        /// </summary>
        Block2 = 17,
        /// <summary>
        /// <remarks>draft-ietf-core-block</remarks>
        /// </summary>
        Block1 = 19,
        /// <summary>
        /// <remarks>draft-ietf-core-block-08</remarks>
        /// </summary>
        Size = 28,
    }
}
