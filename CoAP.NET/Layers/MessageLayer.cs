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
using System.Collections.Generic;
using System.Timers;
using CoAP.Log;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// This class describes the functionality of a CoAP messaging layer. It provides:
    /// 1. Reliable transport of confirmable messages over underlying layers by making use of retransmissions and exponential backoff;
    /// 2. Matching of confirmables to their corresponding ACK/RST;
    /// 3. Detection and cancellation of duplicate messages;
    /// 4. Retransmission of ACK/RST messages upon receiving duplicate confirmable messages.
    /// </summary>
    public class MessageLayer : UpperLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(MessageLayer));
        private static Int32 currentMessageID = (Int32)((new Random()).NextDouble() * 0x10000);
        
        private IDictionary<String, TransmissionContext> _transactionTable = new HashMap<String, TransmissionContext>();
        private HashMap<String, Message> _dupCache = new HashMap<String, Message>();
        private HashMap<String, Message> _replyCache = new HashMap<String, Message>();
        private Object _syncRoot = new Byte[0];

        public static Int32 NextMessageID()
        {
            currentMessageID = ++currentMessageID % 0x10000;
            return currentMessageID;
        }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            // set message ID
            if (msg.ID < 0)
            {
                msg.ID = NextMessageID();
            }

            if (msg is Response && msg.HasOption(OptionType.Observe))
            {
                if (UpdateTransmission(msg))
                    return;
            }
            else if (msg.IsConfirmable)
            {
                // create new transmission context for retransmissions
                AddTransmission(msg);
            }
            else if (msg.IsReply)
            {
                _replyCache[msg.Key] = msg;
            }

            SendMessageOverLowerLayer(msg);
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            // check for support
            if (msg is UnsupportedRequest)
            {
                Message reply = msg.NewReply(msg.IsConfirmable);
                reply.Code = Code.MethodNotAllowed;
                reply.SetPayload(String.Format("MessageLayer - Method code {0} not supported", msg.Code));

                try
                {
                    SendMessageOverLowerLayer(reply);
                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("MessageLayer - Replied to unsupported request code {0}: {1}", msg.Code, msg.Key));
                }
                catch (Exception ex)
                {
                    if (log.IsErrorEnabled)
                        log.Error("MessageLayer - Replying to unsupported request code failed: " + ex.Message);
                }

                return;
            }

            // check for duplicate
            if (_dupCache.ContainsKey(msg.Key))
            {
                // check for retransmitted Confirmable
                if (msg.IsConfirmable)
                {
                    if (msg is Response)
                    {
                        try
                        {
                            if (log.IsDebugEnabled)
                                log.Debug("MessageLayer - Re-acknowledging duplicate response: " + msg.Key);
                            SendMessageOverLowerLayer(msg.NewAccept());
                        }
                        catch (Exception ex)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("MessageLayer - Re-acknowledging duplicate response failed: " + ex.Message);
                        }
                        return;
                    }

                    // retrieve cached reply
                    Message reply = _replyCache[msg.Key];
                    if (reply != null)
                    {
                        // retransmit reply
                        try
                        {
                            SendMessageOverLowerLayer(reply);
                            if (log.IsDebugEnabled)
                                log.Debug("MessageLayer - Replied duplicate confirmable: " + msg.Key);
                        }
                        catch (Exception ex)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("MessageLayer - Replying to duplicate confirmable failed: " + ex.Message);
                        }
                    }
                    else
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("MessageLayer - Dropped duplicate confirmable without cached reply: " + msg.Key);
                    }

                    // drop duplicate anyway
                    return;
                }
                else
                {
                    // ignore duplicate
                    if (log.IsDebugEnabled)
                        log.Debug("MessageLayer - Dropped duplicate : " + msg.Key);
                    return;
                }
            }
            else // !_dupCache.ContainsKey(msg.Key)
            {
                _dupCache[msg.Key] = msg;
            }
            
            if (msg.IsReply)
            {
                // retrieve context to the incoming message
                TransmissionContext ctx = GetTransmission(msg);
                if (ctx != null)
                {
                    // transmission completed
                    RemoveTransmission(ctx);

                    if (msg.IsEmptyACK)
                    {
                        // transaction is complete, no information for higher layers
                        // FIXME still pass the response to higher layers
                        //return;
                    }
                    else if (msg.Type == MessageType.RST)
                    {
                        HandleIncomingReset(msg);
                        return;
                    }
                }
                else if (msg.Type == MessageType.RST)
                {
                    HandleIncomingReset(msg);
                    return;
                }
                else
                {
                    // ignore unexpected reply
                    if (log.IsWarnEnabled)
                        log.Warn("MessageLayer - Dropped unexpected reply: " + msg.Key);
                    return;
                }
            }

            // Only accept Responses here, Requests must be handled at application level
            if (msg is Response && msg.IsConfirmable)
            {
                try
                {
                    if (log.IsDebugEnabled)
                        log.Debug("MessageLayer - Accepted confirmable response: " + msg.Key);
                    SendMessageOverLowerLayer(msg.NewAccept());
                }
                catch (Exception ex)
                {
                    if (log.IsErrorEnabled)
                        log.Error("MessageLayer - Accepted confirmable response failed: " + ex.Message);
                }
            }

            // pass message to registered receivers
            DeliverMessage(msg);
        }

        private void HandleIncomingReset(Message msg)
        { 
            // remove possible observers
            ObservingManager.Instance.RemoveObserver(msg.PeerAddress.ToString(), msg.ID);
        }

        private Boolean UpdateTransmission(Message msg)
        {
            lock (_syncRoot)
            {
                TransmissionContext ctx = null;

                // remove old notifications
                foreach (TransmissionContext check in _transactionTable.Values)
                {
                    if (check.msg is Response)
                    {
                        Response resp = (Response)check.msg;
                        if (resp.HasOption(OptionType.Observe)
                            && resp.PeerAddress.Equals(msg.PeerAddress))
                        {
                            Response ntf = (Response)msg;
                            if (resp.Request != null && ntf.Request != null
                                && resp.Request.UriPath.Equals(ntf.Request.UriPath))
                            {
                                ctx = check;
                                break;
                            }
                        }
                    }
                }

                if (ctx != null)
                {
                    ctx.msg.Payload = msg.Payload;
                    ctx.msg.SetOption(msg.GetFirstOption(OptionType.Observe));

                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("Replaced ongoing CON notification: {0} with {1}", ctx.msg.TransactionKey, msg.TransactionKey));

                    return true;
                }
                else if (msg.IsConfirmable)
                {
                    AddTransmission(msg);
                }

                return false;
            }
        }

        private TransmissionContext AddTransmission(Message msg)
        {
            lock (_syncRoot)
            {
                TransmissionContext ctx = new TransmissionContext();
                ctx.msg = msg;
                ctx.numRetransmit = 0;
                ctx.timeoutHandler = HandleResponseTimeout;
                _transactionTable[msg.TransactionKey] = ctx;

                // schedule first retransmission
                ScheduleRetransmission(ctx);

                if (log.IsDebugEnabled)
                    log.Debug("MessageLayer - Stored new transaction for " + msg.Key);

                return ctx;
            }
        }

        private TransmissionContext GetTransmission(Message msg)
        {
            lock (_syncRoot)
            {
                return _transactionTable[msg.TransactionKey];
            }
        }

        private void RemoveTransmission(TransmissionContext ctx)
        {
            lock (_syncRoot)
            {
                ctx.CancelRetransmission();
                _transactionTable.Remove(ctx.msg.TransactionKey);

                if (log.IsDebugEnabled)
                    log.Debug("MessageLayer - Cleared new transaction for " + ctx.msg.Key);
            }
        }

        private void ScheduleRetransmission(TransmissionContext ctx)
        {
            ctx.StartRetransmission();
        }

        private void HandleResponseTimeout(TransmissionContext ctx)
        {
            if (ctx.numRetransmit < CoapConstants.MaxRetransmit)
            {
                ctx.msg.Retransmissioned = ++ctx.numRetransmit;

                if (log.IsInfoEnabled)
                    log.Info(String.Format("MessageLayer - Retransmitting {0} ({1} of {2}), timeout {3}ms",
                        ctx.msg.Key, ctx.numRetransmit, CoapConstants.MaxRetransmit, ctx.timeout * 2));

                try
                {
                    SendMessageOverLowerLayer(ctx.msg);
                }
                catch (Exception ex)
                {
                    if (log.IsErrorEnabled)
                        log.Error("MessageLayer - Retransmission failed: " + ex.Message, ex);
                    RemoveTransmission(ctx);
                    return;
                }

                ScheduleRetransmission(ctx);
            }
            else
            {
                // cancel transmission
                RemoveTransmission(ctx);
                
                // cancel observations
                ObservingManager.Instance.RemoveObserver(ctx.msg.PeerAddress.ToString());

                if (log.IsDebugEnabled)
                    log.Debug(String.Format("MessageLayer - Transmission of {0} cancelled", ctx.msg.Key));

                ctx.msg.HandleTimeout();
            }
        }

        private class TransmissionContext
        {
            public Message msg;
            public Int32 numRetransmit;
            public Action<TransmissionContext> timeoutHandler;
            public Int32 timeout;
            private Timer timer;

            public TransmissionContext()
            {
                timer = new Timer();
                timer.AutoReset = false;
                timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            }

            public void StartRetransmission()
            {
                CancelRetransmission();

                if (0 == timeout)
                    timeout = InitialTimeout();
                else
                    timeout *= 2;

                timer.Interval = timeout;
                timer.Start();
            }

            public void CancelRetransmission()
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

        private static Random _rand = new Random();

        private static Int32 InitialTimeout()
        {
            Int32 min = CoapConstants.ResponseTimeout;
            Double f = CoapConstants.ResponseRandomFactor;
            return (Int32)(min + min * (f - 1D) * _rand.NextDouble());
        }
    }
}
