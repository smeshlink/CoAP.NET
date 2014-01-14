using System;
using System.Threading;

namespace CoAP.EndPoint
{
    class ThreadPoolDispatcher : LocalEndPoint.SimpleDispatcher
    {
        public static readonly ThreadPoolDispatcher Instance = new ThreadPoolDispatcher();

        private readonly WaitCallback _callback;

        private ThreadPoolDispatcher()
        {
            _callback = Dispatch0;
        }

        public override void Dispatch(Request request)
        {
            ThreadPool.QueueUserWorkItem(_callback, request);
        }

        private void Dispatch0(Object state)
        {
            base.Dispatch((Request)state);
        }
    }
}
