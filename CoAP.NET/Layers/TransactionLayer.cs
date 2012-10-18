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
    /// This class describes the functionality of a CoAP transaction layer. It provides:
    /// 1. Matching of responses to the according requests;
    /// 2. Transaction timeouts, e.g. to limit wait time for separate responses and responses to non-confirmable requests.
    /// </summary>
    public class TransactionLayer : UpperLayer
    {
        private Int32 _transactionTimeout;
        private TokenManager _tokenManager;
        private IDictionary<Option, Transaction> _transactions = new HashMap<Option, Transaction>();

        /// <summary>
        /// Initializes a transaction layer.
        /// </summary>
        /// <param name="tokenManager"></param>
        /// <param name="transactionTimeout"></param>
        public TransactionLayer(TokenManager tokenManager, Int32 transactionTimeout)
        {
            this._tokenManager = tokenManager;
            this._transactionTimeout = transactionTimeout;
        }

        /// <summary>
        /// Initializes a transaction layer.
        /// </summary>
        public TransactionLayer(TokenManager tokenManager)
            : this(tokenManager, CoapConstants.DefaultTransactionTimeout)
        {
        }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            if (msg.RequiresToken)
            {
                msg.Token = this._tokenManager.AcquireToken(true);
            }

            // use overall timeout for clients (e.g., server crash after separate response ACK)
            if (msg is Request)
            {
                AddTransaction((Request)msg);
            }

            SendMessageOverLowerLayer(msg);
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;

                // retrieve token option
                Option token = msg.Token;

                Transaction transaction = GetTransaction(token);
                
                // check for missing token
                if (null == transaction && null == token)
                {
                    if (Log.IsErrorEnabled)
                        Log.Error(this, "Remote endpoint failed to echo token");

                    /* Not good, must consider IP and port, too. */
                    //foreach (Transaction t in _transactions.Values)
                    //{
                    //    if (response.ID.Equals(t.request.ID))
                    //    {
                    //        transaction = t;
                    //        if (Log.IsWarningEnabled)
                    //            Log.Warning(this, "Falling back to buddy matching");
                    //        break;
                    //    }
                    //}

                    if (null == transaction)
                        // let timeout handle the problem
                        return;
                }

                // check if received response needs confirmation
                if (response.IsConfirmable)
                {
                    try
                    {
                        // reply with ACK if response matched to transaction, otherwise reply with RST
                        Message reply = response.NewReply(null != transaction);
                        SendMessageOverLowerLayer(reply);
                    }
                    catch (Exception ex)
                    {
                        if (Log.IsErrorEnabled)
                            Log.Error(this, "Failed to reply to confirmable response {0}: {1}", response.Key, ex.Message);
                    }
                }

                if (null != transaction)
                {
                    // attach request to response
                    response.Request = transaction.request;

                    // cancel timeout
                    if (!response.IsEmptyACK)
                    {
                        // TODO 是否应该移除transaction?
                        // IMPORTANT! observe时不能移除
                        transaction.Stop();
                        //RemoveTransaction(transaction);
                    }

                    // TODO separate observe registry
                    if (msg.GetFirstOption(OptionType.Observe) == null)
                    {
                        RemoveTransaction(transaction);
                    }

                    DeliverMessage(msg);
                }
                else
                {
                    // TODO send RST
                    if (Log.IsWarningEnabled)
                        Log.Warning(this, "Dropping unexpected response: {0}", token);
                }
            }
            else if (msg is Request)
            {
                DeliverMessage(msg);
            }
        }

        private Transaction AddTransaction(Request request)
        {
            Option token = request.Token;
            Transaction transaction = new Transaction(token, request, this._transactionTimeout);
            transaction.timeoutHandler = TransactionTimedOut;
            transaction.Start();
            _transactions[token] = transaction;
            return transaction;
        }

        private Transaction GetTransaction(Option token)
        {
            return null == token ? null : _transactions[token];
        }

        private void RemoveTransaction(Transaction transaction)
        {
            transaction.Stop();
            _transactions.Remove(transaction.token);
            this._tokenManager.ReleaseToken(transaction.token);
        }

        private void TransactionTimedOut(Transaction transaction)
        {
            if (Log.IsWarningEnabled)
                Log.Warning(this, "Transaction timed out: {0}", transaction.token);
            RemoveTransaction(transaction);
            transaction.request.HandleTimeout();
        }

        private class Transaction
        {
            private Timer timer;
            public Option token;
            public Request request;
            public Action<Transaction> timeoutHandler;

            public Transaction(Option token, Request request, Int32 timeout)
            {
                this.token = token;
                this.request = request;
                this.timer = new Timer(timeout);
                this.timer.AutoReset = false;
                this.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            }

            public void Start()
            {
                this.timer.Start();
            }

            public void Stop()
            {
                this.timer.Stop();
            }

            private void timer_Elapsed(Object sender, ElapsedEventArgs e)
            {
                if (null != timeoutHandler)
                    timeoutHandler(this);
            }
        }
    }
}
