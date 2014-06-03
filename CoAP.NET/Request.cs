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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using CoAP.EndPoint.Resources;
using CoAP.Log;

namespace CoAP
{
    /// <summary>
    /// This class describes the functionality of a CoAP Request as
    /// a subclass of a CoAP Message. It provides:
    /// 1. operations to answer a request by a response using respond()
    /// 2. different ways to handle incoming responses: receiveResponse() or Responsed event
    /// </summary>
    public class Request : Message
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(Request));

        private Boolean _multicast;
        private Uri _uri;
        
        private static readonly Response timeoutResponse = new Response();
        private static readonly Int64 startTime = DateTime.Now.Ticks;
        // number of responses to this request
        private Int32 _responseCount;
        private Queue _responseQueue;
        private Boolean _isObserving;
        private Boolean _separateResponseEnabled;
        private Response _currentResponse;
        private LocalResource _resource;
        private readonly DateTime _startTime = DateTime.Now;
        private Int32 _sequenceTimeout;

        /// <summary>
        /// Fired when a response arrives.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Responded;
        public event EventHandler<ResponseEventArgs> Responding;

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        public Request(Int32 method)
            : this(method, true)
        { }

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        /// <param name="method">The method code of the message</param>
        /// <param name="confirmable">True if the request is Confirmable</param>
        public Request(Int32 method, Boolean confirmable)
            : base(confirmable ? MessageType.CON : MessageType.NON, method)
        { }

        /// <summary>
        /// Gets or sets a value indicating whether this request is a multicast request or not.
        /// </summary>
        public Boolean Multicast
        {
            get { return _multicast; }
            set { _multicast = value; }
        }

        static readonly Regex regIP = new Regex("(\\[[0-9a-f:]+\\]|[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})", RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets or sets the URI of this CoAP message.
        /// </summary>
        public Uri URI
        {
            get { return _uri; }
            set
            {
                if (null != value)
                {
                    String host = value.Host;
                    Int32 port = value.Port;

                    // set Uri-Host option if not IP literal
                    if (host != null && !regIP.IsMatch(host)
                        && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                        UriHost = host;

                    if (port >= 0)
                    {
                        if (port != CoapConstants.DefaultPort)
                            UriPort = port;
                    }

                    Destination = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);

                    UriPath = value.AbsolutePath;
                    UriQuery = value.Query;
                    PeerAddress = new EndpointAddress(value);
                }
                _uri = value;
            }
        }

        public String UriHost
        {
            get
            {
                Option host = GetFirstOption(OptionType.UriHost);
                if (host == null)
                {
                    if (Destination != null)
                    {
                        return Destination.ToString();
                    }
                    else
                        return "localhost";
                }
                else
                {
                    return host.StringValue;
                }
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else if (value.Length < 1 || value.Length > 255)
                    throw new ArgumentException("URI-Host option's length must be between 1 and 255 inclusive", "value");
                SetOption(Option.Create(OptionType.UriHost, value));
            }
        }

        public String UriPath
        {
            get { return "/" + Option.Join(GetOptions(OptionType.UriPath), "/"); }
            set { SetOptions(Option.Split(OptionType.UriPath, value, "/")); }
        }

        public IEnumerable<String> UriPaths
        {
            get
            {
                IEnumerable<Option> opts = GetOptions(OptionType.UriPath);
                if (opts != null)
                {
                    foreach (Option opt in opts)
                    {
                        yield return opt.StringValue;
                    }
                }
                yield break;
            }
        }

        public String UriQuery
        {
            get { return Option.Join(GetOptions(OptionType.UriQuery), "&"); }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.StartsWith("?"))
                    value = value.Substring(1);
                SetOptions(Option.Split(OptionType.UriQuery, value, "&"));
            }
        }

        public Int32 UriPort
        {
            get
            {
                Option opt = GetFirstOption(OptionType.UriPort);
                return opt == null ? 0 : opt.IntValue;
            }
            set
            {
                if (value == 0)
                    RemoveOptions(OptionType.UriPort);
                else
                    SetOption(Option.Create(OptionType.UriPort, value));
            }
        }

        public Boolean HasObserve
        {
            get { return HasOption(OptionType.Observe); }
        }

        public Int32 Observe
        {
            get { return GetFirstOption(OptionType.Observe).IntValue; }
        }

        public void SetUri(String uri)
        {
            if (!uri.StartsWith("coap://") && !uri.StartsWith("coaps://"))
                uri = "coap://" + uri;
            URI = new Uri(uri);
        }

        /// <summary>
        /// Sets CoAP's observe option. If the target resource of this request
	    /// responds with a success code and also sets the observe option, it will
        /// send more responses in the future whenever the resource's state changes.
        /// </summary>
        public Request SetObserve()
        {
            SetOption(Option.Create(OptionType.Observe, 0));
            return this;
        }

        /// <summary>
        /// Sets CoAP's observe option to the value of 1 to proactively cancel.
        /// </summary>
        public Request SetObserveCancel()
        {
            SetOption(Option.Create(OptionType.Observe, 1));
            return this;
        }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds after which this request will time out.
        /// If not set, the timeout in <see cref="Layers.TokenLayer"/> will be used.
        /// A value less than 0 indicates an infinite time-out period.
        /// </summary>
        public Int32 SequenceTimeout
        {
            get { return _sequenceTimeout; }
            set { _sequenceTimeout = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the response queue is enabled or disabled.
        /// </summary>
        public Boolean ResponseQueueEnabled
        {
            get { return _responseQueue != null; }
            set
            {
                if (value != ResponseQueueEnabled)
                {
                    _responseQueue = value ? new Queue() : null;
                }
            }
        }

        /// <summary>
        /// Gets or sets if this request is an observing request.
        /// </summary>
        public Boolean IsObserving
        {
            get { return _isObserving; }
            set { _isObserving = value; }
        }

        public Boolean SeparateResponseEnabled
        {
            get { return _separateResponseEnabled; }
            set { _separateResponseEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the local resource associated with this request.
        /// </summary>
        public LocalResource Resource
        {
            get { return _resource; }
            set { _resource = value; }
        }

        /// <summary>
        /// Gets or sets the response to this request.
        /// </summary>
        public Response Response
        {
            get { return _currentResponse; }
            set
            {
                // check for valid CoAP message
                if (value != null && value.PayloadSize > 0)
                {
                    if (CoAP.Code.IsSuccess(value.Code))
                    {
                        if (value.Code == CoAP.Code.Valid || value.Code == CoAP.Code.Deleted)
                        {
                            if (log.IsWarnEnabled)
                                log.Warn(String.Format("Removing payload of {0} response: {1}",
                                    CoAP.Code.ToString(value.Code), value.Key));
                            value.SetPayload(String.Empty, MediaType.Undefined);
                        }
                    }
                    else if (value.ContentType == MediaType.Undefined)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn(String.Format("Removing Content-Format of {0} response: {1}",
                                    CoAP.Code.ToString(value.Code), value.Key));
                        value.ContentType = MediaType.Undefined;
                    }
                }
                _currentResponse = value;
            }
        }

        /// <summary>
        /// Gets the time when a request was issued to calculate Observe counter.
        /// </summary>
        public DateTime StartTime
        {
            get { return _startTime; }
        }

        /// <summary>
        /// Gets the value of a query parameter as a <code>String</code>,
        /// or <code>null</code> if the parameter does not exist.
        /// </summary>
        /// <param name="name">a <code>String</code> specifying the name of the parameter</param>
        /// <returns>a <code>String</code> representing the single value of the parameter</returns>
        public String GetParameter(String name)
        {
            foreach (Option query in GetOptions(OptionType.UriQuery))
            {
                String val = query.StringValue;
                if (String.IsNullOrEmpty(val))
                    continue;
                if (val.StartsWith(name + "="))
                    return val.Substring(name.Length + 1);
            }
            return null;
        }

        /// <summary>
        /// Accepts this message and keeps track of the response count, which
        /// is required to manage MIDs for exchanges over multiple transactions.
        /// </summary>
        public override void Accept()
        {
            _responseCount++;
            base.Accept();
        }

        /// <summary>
        /// Executes the request on the endpoint specified by the URI.
        /// </summary>
        public void Execute()
        {
            Send();
        }

        /// <summary>
        /// Receives a response and blocks until a response is available.
        /// </summary>
        /// <returns></returns>
        public Response ReceiveResponse()
        {
            return ReceiveResponse(true);
        }

        /// <summary>
        /// Receives a response.
        /// </summary>
        /// <param name="waiting">Blocking or not</param>
        /// <returns></returns>
        /// <exception cref="System.Threading.ThreadInterruptedException"></exception>
        public Response ReceiveResponse(Boolean waiting)
        {
            // response queue required to perform this operation
            if (!ResponseQueueEnabled)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Response queue is not enabled, responses may be lost");
                ResponseQueueEnabled = true;
            }

            Response response = null;
            System.Threading.Monitor.Enter(_responseQueue.SyncRoot);
            while (waiting && _responseQueue.Count == 0)
            {
                System.Threading.Monitor.Wait(_responseQueue.SyncRoot);
            }
            if (_responseQueue.Count > 0)
                response = (Response)_responseQueue.Dequeue();
            System.Threading.Monitor.Exit(_responseQueue.SyncRoot);

            return response == timeoutResponse ? null : response;
        }

        public void Respond(Int32 code)
        {
            Respond(code, null);
        }

        public void Respond(Int32 code, String message)
        {
            Response response = new Response(code);
            if (null != message)
                response.SetPayload(message);
            Respond(response);
        }

        public void Respond(Int32 code, String message, Int32 mediaType)
        {
            Response response = new Response(code);
            if (null != message)
                response.SetPayload(message, mediaType);
            Respond(response);
        }

        /// <summary>
        /// Places a new response to this request, e.g. to answer it
        /// </summary>
        /// <param name="response"></param>
        public void Respond(Response response)
        {
            response.Request = this;
            response.Communicator = this.Communicator;

            response.PeerAddress = this.PeerAddress;

            if (_responseCount == 0 && IsConfirmable)
                response.ID = this.ID;

            // TODO 枚举不能与null比较
            //if (null == response.Type)
            {
                if (_responseCount == 0 && IsConfirmable)
                {
                    // use piggy-backed response
                    response.Type = MessageType.ACK;
                }
                else
                {
                    // use separate response:
                    // Confirmable response to confirmable request,
                    // Non-confirmable response to non-confirmable request
                    response.Type = this.Type;
                }
            }

            if (response.Code != CoAP.Code.Empty)
            {
                response.Token = this.Token;
                response.RequiresToken = this.RequiresToken;

                // echo block1 option
                BlockOption block1 = (BlockOption)GetFirstOption(OptionType.Block1);
                if (null != block1)
                {
                    // TODO: block1.setM(false); maybe in TransferLayer
                    response.AddOption(block1);
                }
            }

            _responseCount++;
            Response = response;
            // Endpoint will call SendResponse();
        }

        /// <summary>
        /// Places a response to this request.
        /// </summary>
        /// <param name="response"></param>
        public void HandleResponse(Response response)
        {
            if (SeparateResponseEnabled || !response.IsEmptyACK)
            {
                if (ResponseQueueEnabled)
                {
                    System.Threading.Monitor.Enter(_responseQueue.SyncRoot);
                    _responseQueue.Enqueue(response);
                    System.Threading.Monitor.Pulse(_responseQueue.SyncRoot);
                    System.Threading.Monitor.Exit(_responseQueue.SyncRoot);
                }

                OnResponse(response);
            }
        }

        /// <summary>
        /// Sends the current response.
        /// </summary>
        public void SendResponse()
        {
            if (_currentResponse != null)
            {
                if (!_isObserving)
                {
                    // check if resource is to be observed
                    if (_resource != null && _resource.Observable
                        && Code == CoAP.Code.GET
                        && CoAP.Code.GetResponseClass(Response.Code) == CoAP.Code.SuccessCode)
                    {
                        if (HasOption(OptionType.Observe))
                        {
                            // establish new observation relationship
                            ObservingManager.Instance.AddObserver(this, _resource);
                        }
                        else if (ObservingManager.Instance.IsObserved(PeerAddress.ToString(), _resource))
                        {
                            // terminate observation relationship on that resource
                            ObservingManager.Instance.RemoveObserver(PeerAddress.ToString(), _resource);
                        }
                    }

                    if (PeerAddress == null)
                        // handle locally
                        HandleResponse(_currentResponse);
                    else
                        _currentResponse.Send();
                }
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn(String.Format("Missing response to send: Request {0} for {1}", Key, UriPath));
            }
        }

        /// <summary>
        /// Dispatches this request to the handler.
        /// </summary>
        /// <param name="handler"></param>
        public void Dispatch(IRequestHandler handler)
        {
            DoDispatch(handler);
        }

        protected override void DoHandleTimeout()
        {
            HandleResponse(timeoutResponse);
        }

        protected override void DoHandleBy(IMessageHandler handler)
        {
            handler.HandleMessage(this);
        }

        /// <summary>
        /// Dispatches this request to the handler.
        /// </summary>
        protected virtual void DoDispatch(IRequestHandler handler)
        {
            if (log.IsWarnEnabled)
                log.Warn("Unable to dispatch request with code " + CoAP.Code.ToString(Code));
        }

        private void OnResponse(Response response)
        {
            if (null != Responded)
            {
                Responded(this, new ResponseEventArgs(response == timeoutResponse ? null : response));
            }
        }

        private void OnResponding(Response response)
        {
            if (null != Responding)
            {
                Responding(this, new ResponseEventArgs(response));
            }
        }

        internal void ResponsePayloadAppended(Response response, Byte[] block)
        {
            OnResponding(response);
        }

        /// <summary>
        /// Construct a GET request.
        /// </summary>
        public static Request NewGet()
        {
            return new Request(CoAP.Code.GET);
        }

        /// <summary>
        /// Construct a POST request.
        /// </summary>
        public static Request NewPost()
        {
            return new Request(CoAP.Code.POST);
        }

        /// <summary>
        /// Construct a PUT request.
        /// </summary>
        public static Request NewPut()
        {
            return new Request(CoAP.Code.PUT);
        }

        /// <summary>
        /// Construct a DELETE request.
        /// </summary>
        public static Request NewDelete()
        {
            return new Request(CoAP.Code.DELETE);
        }

        /// <summary>
        /// Creates a request object according to the specified method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static Request Create(Method method)
        {
            switch (method)
            {
                case Method.POST:
                    return new POSTRequest();
                case Method.PUT:
                    return new PUTRequest();
                case Method.DELETE:
                    return new DELETERequest();
                case Method.GET:
                default:
                    return new GETRequest();
            }
        }

        /// <summary>
        /// Methods of request
        /// </summary>
        public enum Method
        {
            /// <summary>
            /// GET method
            /// </summary>
            GET = 1,
            /// <summary>
            /// POST method
            /// </summary>
            POST,
            /// <summary>
            /// PUT method
            /// </summary>
            PUT,
            /// <summary>
            /// DELETE method
            /// </summary>
            DELETE
        }
    }

    /// <summary>
    /// Class for GET request.
    /// </summary>
    public class GETRequest : Request
    {
        public GETRequest()
            : base(CoAP.Code.GET, true)
        { }

        protected override void DoDispatch(IRequestHandler handler)
        {
            handler.DoGet(this);
        }
    }

    /// <summary>
    /// Class for POST request.
    /// </summary>
    public class POSTRequest : Request
    {
        public POSTRequest()
            : base(CoAP.Code.POST, true)
        { }

        protected override void DoDispatch(IRequestHandler handler)
        {
            handler.DoPost(this);
        }
    }

    /// <summary>
    /// Class for PUT request.
    /// </summary>
    public class PUTRequest : Request
    {
        public PUTRequest()
            : base(CoAP.Code.PUT, true)
        { }

        protected override void DoDispatch(IRequestHandler handler)
        {
            handler.DoPut(this);
        }
    }

    /// <summary>
    /// Class for DELETE request.
    /// </summary>
    public class DELETERequest : Request
    {
        public DELETERequest()
            : base(CoAP.Code.DELETE, true)
        { }

        protected override void DoDispatch(IRequestHandler handler)
        {
            handler.DoDelete(this);
        }
    }

    public class UnsupportedRequest : Request
    {
        public UnsupportedRequest(Int32 code)
            : base(code, true)
        { }
    }
}
