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
using CoAP.Log;
using CoAP.Net;
using CoAP.Observe;

namespace CoAP
{
    public class CoapClient
    {
        private static ILogger log = LogManager.GetLogger(typeof(CoapClient));
        private static readonly IEnumerable<WebLink> EmptyLinks = new WebLink[0];
        private Uri _uri;
        private ICoapConfig _config;
        private IEndPoint _endpoint;
        private MessageType _type = MessageType.CON;
        private Int32 _blockwise;
        private Int32 _timeout = System.Threading.Timeout.Infinite;

        public event EventHandler<ResponseEventArgs> Respond;
        public event EventHandler<ErrorEventArgs> Error;

        public CoapClient()
            : this(null, null)
        { }

        public CoapClient(Uri uri)
            : this(uri, null)
        { }

        public CoapClient(ICoapConfig config)
            : this(null, config)
        { }

        public CoapClient(Uri uri, ICoapConfig config)
        {
            _uri = uri;
            _config = config ?? CoapConfig.Default;
        }

        /// <summary>
        /// Gets or sets the destination URI of this client.
        /// </summary>
        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        /// <summary>
        /// Gets or sets the endpoint this client is supposed to use.
        /// </summary>
        public IEndPoint EndPoint
        {
            get { return _endpoint; }
            set { _endpoint = value; }
        }

        /// <summary>
        /// Gets or sets the timeout how long synchronous method calls will wait
        /// until they give up and return anyways. The default value is <see cref="System.Threading.Timeout.Infinite"/>.
        /// </summary>
        public Int32 Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Let the client use Confirmable requests.
        /// </summary>
        public CoapClient UseCONs()
        {
            _type = MessageType.CON;
            return this;
        }

        /// <summary>
        /// Let the client use Non-Confirmable requests.
        /// </summary>
        public CoapClient UseNONs()
        {
            _type = MessageType.NON;
            return this;
        }

        /// <summary>
        /// Let the client use early negotiation for the blocksize
        /// (16, 32, 64, 128, 256, 512, or 1024). Other values will
        /// be matched to the closest logarithm dualis.
        /// </summary>
        public CoapClient UseEarlyNegotiation(Int32 size)
        {
            _blockwise = size;
            return this;
        }

        /// <summary>
        /// Let the client use late negotiation for the block size (default).
        /// </summary>
        public CoapClient UseLateNegotiation()
        {
            _blockwise = 0;
            return this;
        }

        /// <summary>
        /// Performs a CoAP ping.
        /// </summary>
        /// <returns>success of the ping</returns>
        public Boolean Ping()
        {
            return Ping(_timeout);
        }

        /// <summary>
        /// Performs a CoAP ping and gives up after the given number of milliseconds.
        /// </summary>
        /// <param name="timeout">the time to wait for a pong in milliseconds</param>
        /// <returns>success of the ping</returns>
        public Boolean Ping(Int32 timeout)
        {
            try
            {
                Request request = new Request(Code.Empty, true);
                request.Token = CoapConstants.EmptyToken;
                request.URI = Uri;
                request.Send().WaitForResponse(timeout);
                request.Canceled = true;
                return request.Rejected;
            }
            catch (System.Threading.ThreadInterruptedException)
            {
                /* ignore */
            }
            return false;
        }

        public IEnumerable<WebLink> Discover()
        {
            return Discover(null);
        }

        public IEnumerable<WebLink> Discover(String query)
        {
            Request discover = Prepare(Request.NewGet());
            discover.ClearUriPath().ClearUriQuery().UriPath = CoapConstants.DefaultWellKnownURI;
            if (!String.IsNullOrEmpty(query))
                discover.UriQuery = query;
            Response links = discover.Send().WaitForResponse(_timeout);
            if (links == null)
                // if no response, return null (e.g., timeout)
                return null;
            else if (links.ContentFormat != MediaType.ApplicationLinkFormat)
                return EmptyLinks;
            else
                return LinkFormat.Parse(links.PayloadString);
        }

        /// <summary>
        /// Sends a GET request and blocks until the response is available.
        /// </summary>
        /// <returns>the CoAP response</returns>
        public Response Get()
        {
            return Send(Request.NewGet());
        }

        /// <summary>
        /// Sends a GET request with the specified Accept option and blocks
        /// until the response is available.
        /// </summary>
        /// <param name="accept">the Accept option</param>
        /// <returns>the CoAP response</returns>
        public Response Get(Int32 accept)
        {
            return Send(Accept(Request.NewGet(), accept));
        }

        /// <summary>
        /// Sends a GET request asynchronizely.
        /// </summary>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void GetAsync(Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Request.NewGet(), done, fail);
        }

        /// <summary>
        /// Sends a GET request with the specified Accept option asynchronizely.
        /// </summary>
        /// <param name="accept">the Accept option</param>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void GetAsync(Int32 accept, Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept(Request.NewGet(), accept), done, fail);
        }

        public Response Post(String payload, Int32 format = MediaType.TextPlain)
        {
            return Send((Request)Request.NewPost().SetPayload(payload, format));
        }

        public Response Post(String payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPost().SetPayload(payload, format), accept));
        }

        public Response Post(Byte[] payload, Int32 format)
        {
            return Send((Request)Request.NewPost().SetPayload(payload, format));
        }

        public Response Post(Byte[] payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPost().SetPayload(payload, format), accept));
        }

        public void PostAsync(String payload,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            PostAsync(payload, MediaType.TextPlain, done, fail);
        }

        public void PostAsync(String payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPost().SetPayload(payload, format), done, fail);
        }

        public void PostAsync(String payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPost().SetPayload(payload, format), accept), done, fail);
        }

        public void PostAsync(Byte[] payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPost().SetPayload(payload, format), done, fail);
        }

        public void PostAsync(Byte[] payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPost().SetPayload(payload, format), accept), done, fail);
        }

        public Response Put(String payload, Int32 format = MediaType.TextPlain)
        {
            return Send((Request)Request.NewPut().SetPayload(payload, format));
        }

        public Response Put(Byte[] payload, Int32 format, Int32 accept)
        {
            return Send(Accept((Request)Request.NewPut().SetPayload(payload, format), accept));
        }

        public Response PutIfMatch(String payload, Int32 format, params Byte[][] etags)
        {
            return Send(IfMatch((Request)Request.NewPut().SetPayload(payload, format), etags));
        }

        public Response PutIfMatch(Byte[] payload, Int32 format, params Byte[][] etags)
        {
            return Send(IfMatch((Request)Request.NewPut().SetPayload(payload, format), etags));
        }

        public Response PutIfNoneMatch(String payload, Int32 format)
        {
            return Send(IfNoneMatch((Request)Request.NewPut().SetPayload(payload, format)));
        }

        public Response PutIfNoneMatch(Byte[] payload, Int32 format)
        {
            return Send(IfNoneMatch((Request)Request.NewPut().SetPayload(payload, format)));
        }

        public void PutAsync(String payload,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            PutAsync(payload, MediaType.TextPlain, done, fail);
        }

        public void PutAsync(String payload, Int32 format,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync((Request)Request.NewPut().SetPayload(payload, format), done, fail);
        }

        public void PutAsync(Byte[] payload, Int32 format, Int32 accept,
            Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Accept((Request)Request.NewPut().SetPayload(payload, format), accept), done, fail);
        }

        /// <summary>
        /// Sends a DELETE request and waits for the response.
        /// </summary>
        /// <returns>the CoAP response</returns>
        public Response Delete()
        {
            return Send(Request.NewDelete());
        }

        /// <summary>
        /// Sends a DELETE request asynchronizely.
        /// </summary>
        /// <param name="done">the callback when a response arrives</param>
        /// <param name="fail">the callback when an error occurs</param>
        public void DeleteAsync(Action<Response> done = null, Action<FailReason> fail = null)
        {
            SendAsync(Request.NewDelete(), done, fail);
        }

        public Response Validate(params Byte[][] etags)
        {
            return Send(ETags(Request.NewGet(), etags));
        }

        public CoapObserveRelation Observe(Action<Response> notify = null, Action<FailReason> error = null)
        {
            return Observe(Request.NewGet().MarkObserve(), notify, error);
        }

        public CoapObserveRelation Observe(Int32 accept, Action<Response> notify = null, Action<FailReason> error = null)
        {
            return Observe(Accept(Request.NewGet().MarkObserve(), accept), notify, error);
        }

        public CoapObserveRelation ObserveAsync(Action<Response> notify = null, Action<FailReason> error = null)
        {
            return ObserveAsync(Request.NewGet().MarkObserve(), notify, error);
        }

        public CoapObserveRelation ObserveAsync(Int32 accept, Action<Response> notify = null, Action<FailReason> error = null)
        {
            return ObserveAsync(Accept(Request.NewGet().MarkObserve(), accept), notify, error);
        }

        public Response Send(Request request)
        {
            return Prepare(request).Send().WaitForResponse(_timeout);
        }

        public void SendAsync(Request request, Action<Response> done = null, Action<FailReason> fail = null)
        {
            request.Respond += (o, e) => Deliver(done, e);
            request.Reject += (o, e) => Fail(fail, FailReason.Rejected);
            request.Timeout += (o, e) => Fail(fail, FailReason.TimedOut);
            
            Prepare(request).Send();
        }

        protected Request Prepare(Request request)
        {
            request.Type = _type;
            request.URI = _uri;
            
            if (_blockwise != 0)
                request.SetBlock2(BlockOption.EncodeSZX(_blockwise), false, 0);

            if (_endpoint != null)
                request.EndPoint = _endpoint;

            return request;
        }

        private CoapObserveRelation Observe(Request request, Action<Response> notify, Action<FailReason> error)
        {
            ObserveNotificationOrderer orderer = new ObserveNotificationOrderer(_config);
            CoapObserveRelation relation = new CoapObserveRelation(request);

            EventHandler<ResponseEventArgs> onResponse = (o, e) =>
            {
                Response resp = e.Response;
                lock (orderer)
                {
                    if (orderer.IsNew(resp))
                    {
                        relation.Current = resp;
                        Deliver(notify, e);
                    }
                    else
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("Dropping old notification: " + resp);
                    }
                }
            };
            Action<FailReason> fail = r =>
            {
                relation.Canceled = true;
                Fail(error, r);
            };
            EventHandler onReject = (o, e) => fail(FailReason.Rejected);
            EventHandler onTimeout = (o, e) => fail(FailReason.TimedOut);

            request.Respond += onResponse;
            request.Reject += onReject;
            request.Timeout += onTimeout;

            relation.SetHandlers(onResponse, onReject, onTimeout);
            
            Response response = Send(request);
            if (response == null || !response.HasOption(OptionType.Observe))
                relation.Canceled = true;
            relation.Current = response;
            return relation;
        }

        private CoapObserveRelation ObserveAsync(Request request, Action<Response> notify, Action<FailReason> error)
        {
            ObserveNotificationOrderer orderer = new ObserveNotificationOrderer(_config);
            CoapObserveRelation relation = new CoapObserveRelation(request);

            EventHandler<ResponseEventArgs> onResponse = (o, e) =>
            {
                Response resp = e.Response;
                lock (orderer)
                {
                    if (orderer.IsNew(resp))
                    {
                        relation.Current = resp;
                        notify(resp);
                    }
                    else
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("Dropping old notification: " + resp);
                    }
                }
            };
            Action<FailReason> fail = r =>
            {
                relation.Canceled = true;
                error(r);
            };
            EventHandler onReject = (o, e) => fail(FailReason.Rejected);
            EventHandler onTimeout = (o, e) => fail(FailReason.TimedOut);

            request.Respond += onResponse;
            request.Reject += onReject;
            request.Timeout += onTimeout;

            relation.SetHandlers(onResponse, onReject, onTimeout);

            Prepare(request).Send();
            return relation;
        }

        private void Deliver(Action<Response> act, ResponseEventArgs e)
        {
            if (act != null)
                act(e.Response);
            EventHandler<ResponseEventArgs> h = Respond;
            if (h != null)
                h(this, e);
        }

        private void Fail(Action<FailReason> fail, FailReason reason)
        {
            if (fail != null)
                fail(reason);
            EventHandler<ErrorEventArgs> h = Error;
            if (h != null)
                h(this, new ErrorEventArgs(reason));
        }

        static Request Accept(Request request, Int32 accept)
        {
            request.Accept = accept;
            return request;
        }

        static Request IfMatch(Request request, params Byte[][] etags)
        {
            foreach (Byte[] etag in etags)
            {
                request.AddIfMatch(etag);
            }
            return request;
        }

        static Request IfNoneMatch(Request request)
        {
            request.IfNoneMatch = true;
            return request;
        }

        static Request ETags(Request request, params Byte[][] etags)
        {
            foreach (Byte[] etag in etags)
            {
                request.AddETag(etag);
            }
            return request;
        }

        public enum FailReason
        {
            Rejected, TimedOut
        }

        public class ErrorEventArgs : EventArgs
        {
            internal ErrorEventArgs(FailReason reason)
            {
                this.Reason = reason;
            }

            public FailReason Reason { get; private set; }
        }
    }

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
