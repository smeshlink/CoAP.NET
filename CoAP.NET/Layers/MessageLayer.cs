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
using System.Collections.Generic;
using System.Timers;
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
        private Boolean _retransmitEnabled = true;
        private Int32 _messageId;
        private IDictionary<Int32, TransmissionContext> _txTable = new HashMap<Int32, TransmissionContext>();
        // TODO cache需要使用能够自动移除旧entry的数据结构，减少资源使用
        private IDictionary<String, Message> _dupCache = new HashMap<String, Message>();
        private IDictionary<String, Message> _replyCache = new HashMap<String, Message>();
        private Object _syncRoot = new Byte[0];

        /// <summary>
        /// Initializes a message layer.
        /// </summary>
        public MessageLayer()
        {
            this._messageId = (int)((new Random()).NextDouble() * 0x10000);
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

                // schedule first retransmission
                ScheduleRetransmission(ctx);
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
            // check for duplicate
            if (_dupCache.ContainsKey(msg.Key))
            {
                // check for retransmitted Confirmable
                if (msg.IsConfirmable)
                {
                    // retrieve cached reply
                    Message reply = _replyCache[msg.Key];
                    if (reply != null)
                    {
                        // retransmit reply
                        try
                        {
                            SendMessageOverLowerLayer(reply);
                        }
                        catch (Exception ex)
                        {
                            if (Log.IsErrorEnabled)
                                Log.Error(this, ex.Message);
                        }

                        if (Log.IsInfoEnabled)
                            Log.Info(this, "Replied to duplicate Confirmable: {0}", msg.Key);

                        // ignore duplicate
                        return;
                    }
                }
                else
                {
                    // ignore duplicate
                    if (Log.IsInfoEnabled)
                        Log.Info(this, "Duplicate dropped: {0}", msg.Key);
                    return;
                }
            }
            else
            {
                _dupCache[msg.Key] = msg;
            }

            if (msg.IsReply)
            {
                // retrieve context to the incoming message
                TransmissionContext ctx = GetTransmission(msg);
                if (ctx != null)
                {
                    // match reply to corresponding Confirmable
                    Message.MatchBuddies(ctx.msg, msg);

                    // transmission completed
                    RemoveTransmission(ctx);
                }
                else
                {
                    // ignore unexpected reply
                    if (Log.IsWarningEnabled)
                        Log.Warning(this, "Unexpected reply dropped: {0}", msg.Key);
                    return;
                }
            }

            // pass message to registered receivers
            DeliverMessage(msg);
        }

        private Int32 NextMessageID()
        {
            Int32 id = _messageId;
            ++_messageId;
            if (_messageId > Message.MaxID)
            {
                _messageId = 1;
            }
            return id;
        }

        private TransmissionContext AddTransmission(Message msg)
        {
            lock (this._syncRoot)
            {
                TransmissionContext ctx = new TransmissionContext();
                ctx.msg = msg;
                ctx.numRetransmit = 0;
                ctx.timeoutHandler = HandleResponseTimeout;
                _txTable[msg.ID] = ctx;
                return ctx;
            }
        }

        private TransmissionContext GetTransmission(Message msg)
        {
            lock (this._syncRoot)
            {
                return _txTable[msg.ID];
            }
        }

        private void RemoveTransmission(TransmissionContext ctx)
        {
            lock (this._syncRoot)
            {
                ctx.CancelRetransmission();
                _txTable.Remove(ctx.msg.ID);
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
                ++ctx.numRetransmit;

                if (Log.IsInfoEnabled)
                    Log.Info(this, "Retransmitting {0} ({1} of {2})", ctx.msg.Key, ctx.numRetransmit, CoapConstants.MaxRetransmit);

                try
                {
                    SendMessageOverLowerLayer(ctx.msg);
                }
                catch (Exception ex)
                {
                    if (Log.IsErrorEnabled)
                        Log.Error(this, "Retransmission failed: {0}", ex.Message);
                    RemoveTransmission(ctx);
                    return;
                }

                ScheduleRetransmission(ctx);
            }
            else
            {
                RemoveTransmission(ctx);
                if (Log.IsWarningEnabled)
                    Log.Warning(this, "Transmission of {0} cancelled", ctx.msg.Key);
                ctx.msg.HandleTimeout();
            }
        }

        private class TransmissionContext
        {
            private Int32 timeout;
            private Timer timer;
            public Message msg;
            public Int32 numRetransmit;
            public Action<TransmissionContext> timeoutHandler;

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

            void timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                if (null != timeoutHandler)
                    timeoutHandler(this);
            }
        }

        private static Random _rand = new Random();

        private static Int32 Rnd(Int32 min, Int32 max)
        {
            return min + (Int32)(_rand.NextDouble() * (max - min + 1));
        }

        private static Int32 InitialTimeout()
        {
            Int32 min = CoapConstants.ResponseTimeout;
            Double f = CoapConstants.ResponseRandomFactor;
            return Rnd(min, (Int32)(min * f));
        }
    }
}
