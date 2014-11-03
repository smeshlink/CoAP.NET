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
    /// Represents a CoAP observe relation between a CoAP client and a resource on a server.
    /// Provides a simple API to check whether a relation has successfully established and
    /// to cancel or refresh the relation.
    /// </summary>
    public class CoapObserveRelation
    {
        readonly Request _request;
        private Boolean _canceled;
        private Response _current;
        private EventHandler<ResponseEventArgs> _onResponse;
        private EventHandler _onReject;
        private EventHandler _onTimeout;

        internal CoapObserveRelation(Request request)
        {
            _request = request;
        }

        public Response Current
        {
            get { return _current; }
            internal set { _current = value; }
        }

        public Boolean Canceled
        {
            get { return _canceled; }
            set { _canceled = value; }
        }

        public void ReactiveCancel()
        {
            _request.Canceled = true;
            _canceled = true;
        }

        public void ProactiveCancel()
        {
            Request cancel = Request.NewGet();
            // copy options, but set Observe to cancel
            cancel.SetOptions(_request.GetOptions());
            cancel.MarkObserveCancel();
            // use same Token
            cancel.Token = _request.Token;
            cancel.Destination = _request.Destination;

            // dispatch final response to the same message observers
            cancel.Respond += _onResponse;
            cancel.Reject += _onReject;
            cancel.Timeout += _onTimeout;

            cancel.EndPoint = _request.EndPoint;
            cancel.Send();
            // cancel old ongoing request
            _request.Canceled = true;
            _canceled = true;
        }

        internal void SetHandlers(EventHandler<ResponseEventArgs> onResponse,
            EventHandler onReject, EventHandler onTimeout)
        {
            _onResponse = onResponse;
            _onReject = onReject;
            _onTimeout = onTimeout;
        }
    }
}
