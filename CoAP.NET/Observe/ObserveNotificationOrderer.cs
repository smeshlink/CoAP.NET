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
using System.Threading;

namespace CoAP.Observe
{
    /// <summary>
    /// This class holds the state of an observe relation such
    /// as the timeout of the last notification and the current number.
    /// </summary>
    class ObserveNotificationOrderer
    {
        private Int32 _number;
        private Int64 _timestamp;

        /// <summary>
        /// Gets a new observe option number.
        /// </summary>
        /// <returns>a new observe option number</returns>
        public Int32 GetNextObserveNumber()
        {
            Int32 next = Interlocked.Increment(ref _number);
            while (next >= 1 << 24)
            {
                Interlocked.CompareExchange(ref _number, 0, next);
                next = Interlocked.Increment(ref _number);
            }
            return next;
        }

        /// <summary>
        /// Gets the current notification number.
        /// </summary>
        public Int32 Current
        {
            get { return _number; }
        }

        public Boolean IsNew(Response response)
        {
            // Multiple responses with different notification numbers might
            // arrive and be processed by different threads. We have to
            // ensure that only the most fresh one is being delivered.
            // We use the notation from the observe draft-08.
            
            // TODO
            throw new NotImplementedException();
        }
    }
}
