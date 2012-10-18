/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
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
using CoAP.Util;

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
        private static Communicator defaultCommunicator;
        private static readonly Response TIMEOUT_RESPONSE = new Response();
        private static readonly Int64 startTime = DateTime.Now.Ticks;
        private Communicator _communicator;
        // number of responses to this request
        private Int32 _responseCount;
        private Queue _responseQueue;

        /// <summary>
        /// Fired when a response arrives.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Responded;
        public event EventHandler<ResponseEventArgs> Responding;

        /// <summary>
        /// Gets the default communicator used for outgoing requests.
        /// </summary>
        public static Communicator DefaultCommunicator
        {
            get
            {
                if (null == defaultCommunicator)
                {
                    defaultCommunicator = new Communicator();
                }
                return defaultCommunicator;
            }
        }

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        public Request()
        { }

        /// <summary>
        /// Initializes a request message.
        /// </summary>
        /// <param name="code">The method code of the message</param>
        /// <param name="confirmable">True if the request is Confirmable</param>
        public Request(Int32 code, Boolean confirmable)
            : base(confirmable ? MessageType.CON : MessageType.NON, code)
        {
        }

        /// <summary>
        /// Gets or sets the communicator used for this request.
        /// </summary>
        public Communicator Communicator
        {
            get { return _communicator; }
            set { _communicator = value; }
        }

        /// <summary>
        /// Executes the request on the endpoint specified by the URI.
        /// </summary>
        public void Execute()
        {
            Communicator comm = _communicator != null ? _communicator
                  : DefaultCommunicator;
            if (comm != null)
            {
                comm.SendMessage(this);
            }
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public void Cancel()
        {
            Canceled = true;
            HandleTimeout();
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
        public Response ReceiveResponse(Boolean waiting)
        {
            if (!ResponseQueueEnabled)
            {
                Log.Warning(this, "Missing useResponseQueue(true) call, responses may be lost");
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

            return response == TIMEOUT_RESPONSE ? null : response;
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
        /// Places a response to this request.
        /// </summary>
        /// <param name="response"></param>
        public void HandleResponse(Response response)
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

        /// <summary>
        /// Places a new response to this request, e.g. to answer it
        /// </summary>
        /// <param name="response"></param>
        public void Respond(Response response)
        {
            response.Request = this;

            response.URI = this.URI;
            response.SetOption(GetFirstOption(OptionType.Token));
            response.RequiresToken = this.RequiresToken;

            if (_responseCount == 0 && IsConfirmable)
            {
                response.ID = this.ID;
            }

            // echo block1 option
            BlockOption block1 = (BlockOption)GetFirstOption(OptionType.Block1);
            if (null != block1)
            {
                response.AddOption(block1);
            }

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

            // check observe option
            Option observeOpt = GetFirstOption(OptionType.Observe);
            if (null != observeOpt && !response.HasOption(OptionType.Observe))
            {
                // 16-bit second counter
                Int32 secs = (Int32)((DateTime.Now.Ticks - startTime) / 1000) & 0xFFFF;

                response.SetOption(Option.Create(OptionType.Observe, secs));

                if (response.IsConfirmable)
                {
                    response.Type = MessageType.NON;
                }
            }

            // check if response is of remote origin, i.e.
            // was received by a communicator
            if (_communicator != null)
            {
                _communicator.SendMessage(response);
            }
            else
            {
                // handle locally
                response.Handle();
            }

            ++_responseCount;
        }

        public void Respond(Int32 code, String message)
        {
            Response response = new Response(code);
            if (null != message)
            {
                response.SetPayload(message);
            }
            Respond(response);
        }

        public void Respond(Int32 code)
        {
            Respond(code, null);
        }

        public void Accept()
        {
            if (IsConfirmable)
            {
                Response ack = new Response(CoAP.Code.Empty);
                ack.Type = MessageType.ACK;
                Respond(ack);
            }
        }

        public void Reject()
        {
            if (IsConfirmable)
            {
                Response rst = new Response(CoAP.Code.Empty);
                rst.Type = MessageType.RST;
                Respond(rst);
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
            HandleResponse(TIMEOUT_RESPONSE);
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
            if (Log.IsWarningEnabled)
                Log.Warning(this, "Unable to dispatch request with code '{0}'", CoAP.Code.ToString(Code));
        }

        private void OnResponse(Response response)
        {
            if (null != Responded)
            {
                Responded(this, new ResponseEventArgs(response));
            }
        }

        public void OnResponding(Response response)
        {
            if (null != Responding)
            {
                Responding(this, new ResponseEventArgs(response));
            }
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
            handler.PerformGET(this);
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
            handler.PerformPOST(this);
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
            handler.PerformPUT(this);
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
            handler.PerformDELETE(this);
        }
    }
}
