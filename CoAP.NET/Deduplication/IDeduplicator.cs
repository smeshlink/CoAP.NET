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

using CoAP.Net;

namespace CoAP.Deduplication
{
    /// <summary>
    /// Provides methods to detect duplicates.
    /// Notice that CONs and NONs can be duplicates.
    /// </summary>
    public interface IDeduplicator
    {
        void Start();
        void Stop();
        void Clear();
        Exchange FindPrevious(Exchange.KeyID key, Exchange exchange);
        Exchange Find(Exchange.KeyID key);
    }
}
