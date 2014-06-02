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
using CoAP.Log;

namespace CoAP.Deduplication
{
    static class DeduplicatorFactory
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(DeduplicatorFactory));

        public static IDeduplicator CreateDeduplicator(ICoapConfig config)
        {
            String type = config.Deduplicator;
            if (String.Equals("MarkAndSweep", type, StringComparison.OrdinalIgnoreCase))
                return new SweepDeduplicator(config);
            else if (String.Equals("CropRotation", type, StringComparison.OrdinalIgnoreCase))
                return new CropRotation(config);
            else if (!String.Equals("Noop", type, StringComparison.OrdinalIgnoreCase))
            {
                if (log.IsWarnEnabled)
                    log.Warn("Unknown deduplicator type: " + type);
            }
            return new NoopDeduplicator();
        }
    }
}
