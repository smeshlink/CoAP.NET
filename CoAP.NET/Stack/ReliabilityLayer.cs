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
using System.Timers;
using CoAP.Log;
using CoAP.Net;

namespace CoAP.Stack
{
    /// <summary>
    /// The reliability layer
    /// </summary>
    public class ReliabilityLayer : AbstractLayer
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(ReliabilityLayer));
        static readonly Object TransmissionContextKey = "TransmissionContext";

        private readonly Random _rand = new Random();
        private ICoapConfig _config;

        /// <summary>
        /// Constructs a new reliability layer.
        /// </summary>
        public ReliabilityLayer(ICoapConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// // Schedules a retransmission for confirmable messages.
        /// </summary>
        public override void SendRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (request.Type == MessageType.Unknown)
                request.Type = MessageType.CON;

            if (request.Type == MessageType.CON)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Scheduling retransmission for " + request);
                PrepareRetransmission(exchange, request, ctx => SendRequest(nextLayer, exchange, request));
            }

            base.SendRequest(nextLayer, exchange, request);
        }

        /// <summary>
        /// Makes sure that the response type is correct. The response type for a NON
	    /// can be NON or CON. The response type for a CON should either be an ACK
	    /// with a piggy-backed response or, if an empty ACK has already be sent, a
        /// CON or NON with a separate response.
        /// </summary>
        public override void SendResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            MessageType mt = response.Type;
            if (mt == MessageType.Unknown)
            {
                MessageType reqType = exchange.CurrentRequest.Type;
                if (reqType == MessageType.CON)
                {
                    if (exchange.CurrentRequest.Acknowledged)
                    {
                        // send separate response
                        response.Type = MessageType.CON;
                    }
                    else
                    {
                        exchange.CurrentRequest.Acknowledged = true;
                        // send piggy-backed response
                        response.Type = MessageType.ACK;
                        response.ID = exchange.CurrentRequest.ID;
                    }
                }
                else
                {
                    // send NON response
                    response.Type = MessageType.NON;
                }
            }
            else if (mt == MessageType.ACK || mt == MessageType.RST)
            {
                response.ID = exchange.CurrentRequest.ID;
            }

            if (response.Type == MessageType.CON)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Scheduling retransmission for " + response);
                PrepareRetransmission(exchange, response, ctx => SendResponse(nextLayer, exchange, response));
            }

            base.SendResponse(nextLayer, exchange, response);
        }

        /// <summary>
        /// When we receive a duplicate of a request, we stop it here and do not
	    /// forward it to the upper layer. If the server has already sent a response,
	    /// we send it again. If the request has only been acknowledged (but the ACK
	    /// has gone lost or not reached the client yet), we resent the ACK. If the
        /// request has neither been responded, acknowledged or rejected yet, the
	    /// server has not yet decided what to do with the request and we cannot do
	    /// anything.
        /// </summary>
        public override void ReceiveRequest(INextLayer nextLayer, Exchange exchange, Request request)
        {
            if (request.Duplicate)
            {
                // Request is a duplicate, so resend ACK, RST or response
                if (exchange.CurrentRequest != null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Respond with the current response to the duplicate request");
                    base.SendResponse(nextLayer, exchange, exchange.CurrentResponse);
                }
                else if (exchange.CurrentRequest.Acknowledged)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("The duplicate request was acknowledged but no response computed yet. Retransmit ACK.");
                    EmptyMessage ack = EmptyMessage.NewACK(request);
                    SendEmptyMessage(nextLayer, exchange, ack);
                }
                else if (exchange.CurrentRequest.Rejected)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("The duplicate request was rejected. Reject again.");
                    EmptyMessage rst = EmptyMessage.NewRST(request);
                    SendEmptyMessage(nextLayer, exchange, rst);
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug("The server has not yet decided what to do with the request. We ignore the duplicate.");
                    // The server has not yet decided, whether to acknowledge or
                    // reject the request. We know for sure that the server has
                    // received the request though and can drop this duplicate here.
                }
            }
            else
            {
                // Request is not a duplicate
                exchange.CurrentRequest = request;
                base.ReceiveRequest(nextLayer, exchange, request);
            }
        }

        /// <summary>
        /// When we receive a Confirmable response, we acknowledge it and it also
	    /// counts as acknowledgment for the request. If the response is a duplicate,
        /// we stop it here and do not forward it to the upper layer.
        /// </summary>
        public override void ReceiveResponse(INextLayer nextLayer, Exchange exchange, Response response)
        {
            TransmissionContext ctx = exchange.Get<TransmissionContext>(TransmissionContextKey);
            if (ctx != null)
            {
                exchange.CurrentRequest.Acknowledged = true;
                ctx.Cancel();
            }

            if (response.Type == MessageType.CON && !exchange.Request.Canceled)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Response is confirmable, send ACK.");
                EmptyMessage ack = EmptyMessage.NewACK(response);
                SendEmptyMessage(nextLayer, exchange, ack);
            }

            if (response.Duplicate)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Response is duplicate, ignore it.");
            }
            else
            {
                base.ReceiveResponse(nextLayer, exchange, response);
            }
        }

        /// <summary>
        /// If we receive an ACK or RST, we mark the outgoing request or response
        /// as acknowledged or rejected respectively and cancel its retransmission.
        /// </summary>
        public override void ReceiveEmptyMessage(INextLayer nextLayer, Exchange exchange, EmptyMessage message)
        {
            switch (message.Type)
            {
                case MessageType.ACK:
                    if (exchange.Origin == Origin.Local)
                        exchange.CurrentRequest.Acknowledged = true;
                    else
                        exchange.CurrentResponse.Acknowledged = true;
                    break;
                case MessageType.RST:
                    if (exchange.Origin == Origin.Local)
                        exchange.CurrentRequest.Rejected = true;
                    else
                        exchange.CurrentResponse.Rejected = true;
                    break;
                default:
                    if (log.IsWarnEnabled)
                        log.Warn("Empty messgae was not ACK nor RST: " + message);
                    break;
            }

            TransmissionContext ctx = exchange.Get<TransmissionContext>(TransmissionContextKey);
            if (ctx != null)
                ctx.Cancel();

            base.ReceiveEmptyMessage(nextLayer, exchange, message);
        }

        private void PrepareRetransmission(Exchange exchange, Message msg, Action<TransmissionContext> retransmit)
        {
            TransmissionContext ctx = exchange.GetOrAdd<TransmissionContext>(
                TransmissionContextKey, _ => new TransmissionContext(exchange, msg, retransmit));
            
            if (ctx.FailedTransmissionCount > 0)
            {
                ctx.CurrentTimeout *= _config.AckTimeoutScale;
            }
            else if (ctx.CurrentTimeout == 0)
            {
                ctx.CurrentTimeout = InitialTimeout(_config.AckTimeout, _config.AckRandomFactor);
            }

            if (log.IsDebugEnabled)
                log.Debug("Send request, failed transmissions: " + ctx.FailedTransmissionCount);

            ctx.Start();
        }

        private Int32 InitialTimeout(Int32 initialTimeout, Double factor)
        {
            return (Int32)(initialTimeout + initialTimeout * (factor - 1D) * _rand.NextDouble());
        }

        class TransmissionContext : IDisposable
        {
            readonly Exchange _exchange;
            readonly Message _message;
            private Int32 _currentTimeout;
            private Int32 _failedTransmissionCount;
            private Timer _timer;
            private Action<TransmissionContext> _retransmit;

            public TransmissionContext(Exchange exchange, Message message, Action<TransmissionContext> retransmit)
            {
                _exchange = exchange;
                _message = message;
                _retransmit = retransmit;
                _currentTimeout = message.AckTimeout;
                _timer = new Timer();
                _timer.AutoReset = false;
                _timer.Elapsed += timer_Elapsed;
            }

            public Int32 FailedTransmissionCount
            {
                get { return _failedTransmissionCount; }
                set { _failedTransmissionCount = value; }
            }

            public Int32 CurrentTimeout
            {
                get { return _currentTimeout; }
                set { _currentTimeout = value; }
            }

            public void Start()
            {
                _timer.Stop();

                if (_currentTimeout > 0)
                {
                    _timer.Interval = _currentTimeout;
                    _timer.Start();
                }
            }

            public void Cancel()
            {
                if (log.IsDebugEnabled)
                    log.Debug("Cancel retransmission.");
                _timer.Stop();
                Dispose();
            }

            public void Dispose()
            {
                _timer.Dispose();
            }

            void timer_Elapsed(Object sender, ElapsedEventArgs e)
            {
                /*
			     * Do not retransmit a message if it has been acknowledged,
			     * rejected, canceled or already been retransmitted for the maximum
			     * number of times.
			     */
                Int32 failedCount = ++_failedTransmissionCount;

                if (_message.Acknowledged)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Timeout: message already acknowledged, cancel retransmission of " + _message);
                    return;
                }
                else if (_message.Rejected)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Timeout: message already rejected, cancel retransmission of " + _message);
                    return;
                }
                else if (_message.Canceled)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Timeout: canceled (ID=" + _message.ID + "), do not retransmit");
                    return;
                }
                else if (failedCount < (_message.MaxRetransmit < 0 ? CoapConstants.MaxRetransmit : _message.MaxRetransmit))
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Timeout: retransmit message, failed: " + failedCount + ", message: " + _message);

                    _message.FireRetransmitting();

                    // message might have canceled
                    if (!_message.Canceled)
                        _retransmit(this);
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Timeout: retransmission limit reached, exchange failed, message: " + _message);
                    _exchange.TimedOut = true;
                    _message.TimedOut = true;
                    _exchange.Remove(TransmissionContextKey);
                }
            }
        }
    }
}
