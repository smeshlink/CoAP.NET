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
using System.Timers;
using CoAP.Log;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// This class takes care of unique tokens for each sequence of request/response exchanges.
    /// Additionally, the TokenLayer takes care of an overall timeout for each request/response exchange.
    /// </summary>
    public class TokenLayer : UpperLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(TokenLayer));

        private HashMap<String, RequestResponseSequence> _exchanges = new HashMap<String, RequestResponseSequence>();
        private Int32 _sequenceTimeout;

        public TokenLayer()
            : this(CoapConstants.DefaultOverallTimeout)
        { }

        public TokenLayer(Int32 sequenceTimeout)
        {
            _sequenceTimeout = sequenceTimeout;
        }

        protected override void DoSendMessage(Message msg)
        {
            if (msg.RequiresToken)
                msg.Token = TokenManager.Instance.AcquireToken(true);

            // use overall timeout for clients (e.g., server crash after separate response ACK)
            if (msg is Request)
            {
                if (log.IsDebugEnabled)
                    log.Debug(String.Format("TokenLayer - Requesting response for {0}: {1}", msg.UriPath, msg.SequenceKey));
                AddExchange((Request)msg);
            }
            else if (msg.Code == Code.Empty)
            {
                if (log.IsDebugEnabled)
                {
                    if (msg.Type == MessageType.RST)
                        log.Debug("TokenLayer - Rejecting request: " + msg.Key);
                    else
                        log.Debug("TokenLayer - Accepting request: " + msg.Key);
                }
            }
            else
            {
                if (log.IsDebugEnabled)
                    log.Debug("TokenLayer - Responding request: " + msg.SequenceKey);
            }

            SendMessageOverLowerLayer(msg);
        }

        protected override void DoReceiveMessage(Message msg)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;
                RequestResponseSequence sequence = GetExchange(msg.SequenceKey);

                if (sequence == null && response.Token.Length == 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("TokenLayer - Remote endpoint failed to echo token: " + msg.Key);
                    
                    // TODO try to recover from peerAddress

                    // let timeout handle the problem
                    return;
                }
                else if (sequence != null)
                {
                    if (!msg.IsEmptyACK)
                    {
                        sequence.Cancel();

                        // TODO separate observe registry
                        if (msg.GetFirstOption(OptionType.Observe) == null)
                        {
                            RemoveExchange(msg.SequenceKey);
                        }
                    }

                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("TokenLayer - Incoming response from {0}: {1} // RTT: {2}ms",
                            ((Response)msg).Request.UriPath, msg.SequenceKey, ((Response)msg).RTT));

                    DeliverMessage(msg);
                }
                else
                {
                    if (log.IsWarnEnabled)
                        log.Warn("TokenLayer - Dropping unexpected response: " + response.SequenceKey);
                }
            }
            else if (msg is Request)
            {
                if (log.IsDebugEnabled)
                    log.Debug("TokenLayer - Incoming request: " + msg.SequenceKey);
                DeliverMessage(msg);
            }
        }

        private RequestResponseSequence AddExchange(Request request)
        {
            // be aware when manually setting tokens, as request/response will be replace
            RemoveExchange(request.SequenceKey);

            RequestResponseSequence sequence = new RequestResponseSequence();
            sequence.key = request.SequenceKey;
            sequence.request = request;
            sequence.timeoutHandler = RequestTimeout;

            lock (_exchanges)
            {
                _exchanges[sequence.key] = sequence;
            }

            if (_sequenceTimeout >= 0)
                sequence.Start(_sequenceTimeout);

            if (log.IsDebugEnabled)
                log.Debug("TokenLayer - Stored new exchange: " + sequence.key);

            return sequence;
        }

        private void RemoveExchange(String key)
        {
            lock (_exchanges)
            {
                if (_exchanges.ContainsKey(key))
                {
                    RequestResponseSequence exchange = _exchanges[key];
                    _exchanges.Remove(key);
                    exchange.Cancel();
                    TokenManager.Instance.ReleaseToken(exchange.request.Token);
                    if (log.IsDebugEnabled)
                        log.Debug("TokenLayer - Cleared exchange: " + exchange.key);
                }
            }
        }

        private RequestResponseSequence GetExchange(String key)
        {
            lock (_exchanges)
            {
                return _exchanges[key];
            }
        }

        private void RequestTimeout(RequestResponseSequence seq)
        {
            RemoveExchange(seq.key);
            if (log.IsWarnEnabled)
                log.Warn("TokenLayer - Request/Response exchange timed out: " + seq.key);
            seq.request.HandleTimeout();
        }

        class RequestResponseSequence
        {
            public String key;
            public Request request;
            private Timer timer;
            public Action<RequestResponseSequence> timeoutHandler;

            public RequestResponseSequence()
            {
                timer = new Timer();
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            }

            public void Start(Int32 timeout)
            {
                timer.Interval = timeout;
                timer.Start();
            }

            public void Cancel()
            {
                if (timer.Enabled)
                    timer.Stop();
            }

            void timer_Elapsed(Object sender, ElapsedEventArgs e)
            {
                if (null != timeoutHandler)
                    timeoutHandler(this);
            }
        }
    }
}
