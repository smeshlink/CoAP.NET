/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
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
    /// This class describes the functionality of a CoAP message layer. It provides:
    /// 1. Reliable transport of Confirmable messages over underlying layers by making use of retransmissions and exponential backoff;
    /// 2. Matching of Confirmables to their corresponding Acknowledgement/Reset;
    /// 3. Detection and cancellation of duplicate messages;
    /// 4. Retransmission of Acknowledgements/Reset messages upon receiving duplicate Confirmable messages.
    /// </summary>
    public class MessageLayer : UpperLayer
    {
        private static ILogger log = LogManager.GetLogger(typeof(MessageLayer));
        private static Int32 currentMessageID = (Int32)((new Random()).NextDouble() * 0x10000);
        
        private Boolean _retransmitEnabled = true;
        private Int32 _messageId;
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

            // check if message needs confirmation, i.e. a reply is expected
            if (msg.IsConfirmable)
            {
                // create new transmission context
                // to keep track of the Confirmable
                TransmissionContext ctx = AddTransmission(msg);
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
                            msg.Accept();
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
                        return;
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
            // TODO remove possible observers
        }

        private TransmissionContext AddTransmission(Message msg)
        {
            lock (this._syncRoot)
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
            lock (this._syncRoot)
            {
                return _transactionTable[msg.TransactionKey];
            }
        }

        private void RemoveTransmission(TransmissionContext ctx)
        {
            lock (this._syncRoot)
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
            if (this._retransmitEnabled && ctx.numRetransmit < CoapConstants.MaxRetransmit)
            {
                ctx.msg.Retransmissioned = ++ctx.numRetransmit;

                if (log.IsInfoEnabled)
                    log.Info(String.Format("MessageLayer - Retransmitting {0} ({1} of {2})", ctx.msg.Key, ctx.numRetransmit, CoapConstants.MaxRetransmit));

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
                
                // TODO cancel observations

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
            private Int32 timeout;
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
