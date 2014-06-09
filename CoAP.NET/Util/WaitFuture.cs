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
using System.Collections.Generic;

namespace CoAP.Util
{
    public class WaitFuture<TRequest, TResponse> : IDisposable
    {
        private readonly TRequest _request;
        private TResponse _response;
        private System.Threading.ManualResetEvent _mre = new System.Threading.ManualResetEvent(false);

        public WaitFuture(TRequest request)
        {
            _request = request;
        }

        public TRequest Request
        {
            get { return _request; }
        }

        public TResponse Response
        {
            get { return _response; }
            set
            {
                _response = value;
                try { _mre.Set(); }
                catch (ObjectDisposedException) { /* do nothing */ }
            }
        }

        public void Wait()
        {
            _mre.WaitOne();
        }

        public void Wait(Int32 millisecondsTimeout)
        {
            _mre.WaitOne(millisecondsTimeout);
        }

        public void Dispose()
        {
            ((IDisposable)_mre).Dispose();
        }

        public static void WaitAll(IEnumerable<WaitFuture<TRequest, TResponse>> futures)
        {
            foreach (var f in futures)
            {
                f.Wait();
            }
        }

        public static void WaitAll(IEnumerable<WaitFuture<TRequest, TResponse>> futures, Int32 millisecondsTimeout)
        {
            foreach (var f in futures)
            {
                f.Wait(millisecondsTimeout);
            }
        }
    }
}
