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
using System.Collections.Concurrent;
using System.Collections.Generic;
using CoAP.Deduplication;
using CoAP.Log;

namespace CoAP.Net
{
    class Matcher : IMatcher, IDisposable
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(Matcher));

        readonly IDictionary<Exchange.KeyID, Exchange> _exchangesByID       // for outgoing
            = new ConcurrentDictionary<Exchange.KeyID, Exchange>();
        readonly IDictionary<Exchange.KeyToken, Exchange> _exchangesByToken
            = new ConcurrentDictionary<Exchange.KeyToken, Exchange>();
        readonly IDictionary<Exchange.KeyUri, Exchange> _ongoingExchanges   // for blockwise
            = new ConcurrentDictionary<Exchange.KeyUri, Exchange>();
        private Int32 _running;
        private Int32 _currentID;
        private IDeduplicator _deduplicator;

        public Matcher(ICoapConfig config)
        {
            _deduplicator = DeduplicatorFactory.CreateDeduplicator(config);
            if (config.UseRandomIDStart)
                _currentID = new Random().Next(1 << 16);
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) > 0)
                return;
            _deduplicator.Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (System.Threading.Interlocked.Exchange(ref _running, 0) == 0)
                return;
            _deduplicator.Stop();
            Clear();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _exchangesByID.Clear();
            _exchangesByToken.Clear();
            _ongoingExchanges.Clear();
            _deduplicator.Clear();
        }

        /// <inheritdoc/>
        public void SendRequest(Exchange exchange, Request request)
        {
            if (request.ID == Message.None)
                request.ID = System.Threading.Interlocked.Increment(ref _currentID) % (1 << 16);

            /*
             * The request is a CON or NCON and must be prepared for these responses
             * - CON  => ACK/RST/ACK+response/CON+response/NCON+response
             * - NCON => RST/CON+response/NCON+response
             * If this request goes lost, we do not get anything back.
             */

            Exchange.KeyID keyID = new Exchange.KeyID(request.ID, request.Destination);
            Exchange.KeyToken keyToken = new Exchange.KeyToken(request.Token, request.Destination);

            exchange.Completed += OnExchangeCompleted;

            if (log.IsDebugEnabled)
                log.Debug("Stored open request by " + keyID + ", " + keyToken);

            _exchangesByID[keyID] = exchange;
            _exchangesByToken[keyToken] = exchange;
        }

        /// <inheritdoc/>
        public void SendResponse(Exchange exchange, Response response)
        {
            if (response.ID == Message.None)
                response.ID = System.Threading.Interlocked.Increment(ref _currentID) % (1 << 16);

            /*
             * The response is a CON or NON or ACK and must be prepared for these
             * - CON  => ACK/RST // we only care to stop retransmission
             * - NCON => RST // we don't care
             * - ACK  => nothing!
             * If this response goes lost, we must be prepared to get the same 
             * CON/NCON request with same MID again. We then find the corresponding
             * exchange and the retransmissionlayer resends this response.
             */

            if (response.Destination == null)
                throw new InvalidOperationException("Response has no destination set");

            // Insert CON and NON to match ACKs and RSTs to the exchange
            Exchange.KeyID keyID = new Exchange.KeyID(response.ID, response.Destination);
            _exchangesByID[keyID] = exchange;

            if (response.HasOption(OptionType.Block2))
            {
                Request request = exchange.Request;
                Exchange.KeyUri keyUri = new Exchange.KeyUri(request.URI, response.Destination);
                if (exchange.ResponseBlockStatus != null && !response.HasOption(OptionType.Observe))
                {
                    // Remember ongoing blockwise GET requests
                    if (log.IsDebugEnabled)
                        log.Debug("Ongoing Block2 started, storing " + keyUri + "\nOngoing " + request + "\nOngoing " + response);
                    _ongoingExchanges[keyUri] = exchange;
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Ongoing Block2 completed, cleaning up " + keyUri + "\nOngoing " + request + "\nOngoing " + response);
                    _ongoingExchanges.Remove(keyUri);
                }
            }

            if (response.Type == MessageType.ACK || response.Type == MessageType.NON)
            {
                // Since this is an ACK or NON, the exchange is over with sending this response.
                if (response.Last)
                {
                    exchange.Complete = true;
                }
            } // else this is a CON and we need to wait for the ACK or RST
        }

        /// <inheritdoc/>
        public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
        {
            if (message.Type == MessageType.RST && exchange != null)
            {
                // We have rejected the request or response
                exchange.Complete = true;
            }

            /*
             * We do not expect any response for an empty message
             */
            if (message.ID == Message.None && log.IsWarnEnabled)
                log.Warn("Empy message " + message + " has no ID // debugging");
        }

        /// <inheritdoc/>
        public Exchange ReceiveRequest(Request request)
        {
            /*
		     * This request could be
		     *  - Complete origin request => deliver with new exchange
		     *  - One origin block        => deliver with ongoing exchange
		     *  - Complete duplicate request or one duplicate block (because client got no ACK) 
		     *      =>
		     * 		if ACK got lost => resend ACK
		     * 		if ACK+response got lost => resend ACK+response
		     * 		if nothing has been sent yet => do nothing
		     * (Retransmission is supposed to be done by the retransm. layer)
		     */

            Exchange.KeyID keyId = new Exchange.KeyID(request.ID, request.Source);

            /*
             * The differentiation between the case where there is a Block1 or
             * Block2 option and the case where there is none has the advantage that
             * all exchanges that do not need blockwise transfer have simpler and
             * faster code than exchanges with blockwise transfer.
             */
            if (!request.HasOption(OptionType.Block1) && !request.HasOption(OptionType.Block2))
            {
                Exchange exchange = new Exchange(request, Origin.Remote);
                Exchange previous = _deduplicator.FindPrevious(keyId, exchange);
                if (previous == null)
                {
                    exchange.Completed += OnExchangeCompleted;
                    return exchange;
                }
                else
                {
                    if (log.IsInfoEnabled)
                        log.Info("Message is a duplicate, ignore: " + request);
                    request.Duplicate = true;
                    return previous;
                }
            }
            else
            {
                Exchange.KeyUri keyUri = new Exchange.KeyUri(request.URI, request.Source);

                if (log.IsDebugEnabled)
                    log.Debug("Lookup ongoing exchange for " + keyUri);
                Exchange ongoing;
                if (_ongoingExchanges.TryGetValue(keyUri, out ongoing))
                {
                    Exchange prev = _deduplicator.FindPrevious(keyId, ongoing);
                    if (prev != null)
                    {
                        if (log.IsInfoEnabled)
                            log.Info("Message is a duplicate: " + request);
                        request.Duplicate = true;
                    }
                    return ongoing;
                }
                else
                {
                    // We have no ongoing exchange for that request block. 
                    /*
                     * Note the difficulty of the following code: The first message
                     * of a blockwise transfer might arrive twice due to a
                     * retransmission. The new Exchange must be inserted in both the
                     * hash map 'ongoing' and the deduplicator. They must agree on
                     * which exchange they store!
                     */

                    Exchange exchange = new Exchange(request, Origin.Remote);
                    Exchange previous = _deduplicator.FindPrevious(keyId, exchange);
                    if (log.IsDebugEnabled)
                        log.Debug("New ongoing exchange for remote Block1 request with key " + keyUri);
                    if (previous == null)
                    {
                        exchange.Completed += OnExchangeCompleted;
                        _ongoingExchanges[keyUri] = exchange;
                        return exchange;
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                            log.Info("Message is a duplicate: " + request);
                        request.Duplicate = true;
                        return previous;
                    }
                } // if ongoing
            } // if blockwise
        }

        /// <inheritdoc/>
        public Exchange ReceiveResponse(Response response)
        {
            /*
		     * This response could be
		     * - The first CON/NCON/ACK+response => deliver
		     * - Retransmitted CON (because client got no ACK)
		     * 		=> resend ACK
		     */

            Exchange.KeyID keyId = new Exchange.KeyID(response.ID, response.Source);
            Exchange.KeyToken keyToken = new Exchange.KeyToken(response.Token, response.Source);

            Exchange exchange;
            if (_exchangesByToken.TryGetValue(keyToken, out exchange))
            {
                // There is an exchange with the given token
                Exchange prev = _deduplicator.FindPrevious(keyId, exchange);
                if (prev != null)
                {
                    // (and thus it holds: prev == exchange)
                    if (log.IsDebugEnabled)
                        log.Debug("Duplicate response " + response);
                    response.Duplicate = true;
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Exchange got reply: Cleaning up " + keyId);
                    _exchangesByID.Remove(keyId);
                }

                if (response.Type == MessageType.ACK && exchange.CurrentRequest.ID != response.ID)
                {
                    // The token matches but not the MID. This is a response for an older exchange
                    if (log.IsWarnEnabled)
                        log.Warn("Token matches but not MID: Expected " + exchange.CurrentRequest.ID + " but was " + response.ID);
                    // ignore response
                    return null;
                }
                else
                {
                    // this is a separate response that we can deliver
                    return exchange;
                }
            }
            else
            {
                // There is no exchange with the given token.
                if (response.Type != MessageType.ACK)
                {
                    if (log.IsInfoEnabled)
                        log.Info("Response with unknown Token " + keyToken + ": Rejecting " + response);
                    // This is a totally unexpected response.
                    EmptyMessage rst = EmptyMessage.NewRST(response);
                    SendEmptyMessage(exchange, rst);
                }
                // ignore response
                return null;
            }
        }

        /// <inheritdoc/>
        public Exchange ReceiveEmptyMessage(EmptyMessage message)
        {
            Exchange.KeyID keyID = new Exchange.KeyID(message.ID, message.Source);
            Exchange exchange;
            if (_exchangesByID.TryGetValue(keyID, out exchange))
            {
                if (log.IsDebugEnabled)
                    log.Debug("Exchange got reply: Cleaning up " + keyID);
                _exchangesByID.Remove(keyID);
                return exchange;
            }
            else
            {
                if (log.IsInfoEnabled)
                    log.Info("Matcher received empty message that does not match any exchange: " + message);
                // ignore message;
                return null;
            } // else, this is an ACK for an unknown exchange and we ignore it
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            IDisposable d = _deduplicator as IDisposable;
            if (d != null)
                d.Dispose();
        }

        private void OnExchangeCompleted(Object sender, EventArgs e)
        {
            Exchange exchange = (Exchange)sender;

            /* 
			 * Logging in this method leads to significant performance loss.
			 * Uncomment logging code only for debugging purposes.
			 */

            if (exchange.Origin == Origin.Local)
            {
                // this endpoint created the Exchange by issuing a request
                Request request = exchange.Request;
                Exchange.KeyToken keyToken = new Exchange.KeyToken(exchange.CurrentRequest.Token, request.Destination);
                Exchange.KeyID keyID = new Exchange.KeyID(request.ID, request.Destination);

                if (log.IsDebugEnabled)
                    log.Debug("Exchange completed: Cleaning up " + keyToken);

                _exchangesByToken.Remove(keyToken);
                // in case an empty ACK was lost
                _exchangesByID.Remove(keyID);
            }
            else
            {
                // this endpoint created the Exchange to respond a request
                Request request = exchange.CurrentRequest;
                if (request != null)
                {
                    // TODO: We can optimize this and only do it, when the request really had blockwise transfer
                    Exchange.KeyUri uriKey = new Exchange.KeyUri(request.URI, request.Source);
                    //if (log.IsDebugEnabled)
                    //    log.Debug("++++++++++++++++++Remote ongoing completed, cleaning up "+uriKey);
                    _ongoingExchanges.Remove(uriKey);
                }

                // TODO: What if the request is only a block?
                // TODO: This should only happen if the transfer was blockwise

                Response response = exchange.Response;
                if (response != null)
                {
                    // only response MIDs are stored for ACK and RST, no reponse Tokens
                    Exchange.KeyID midKey = new Exchange.KeyID(response.ID, response.Destination);
                    //if (log.IsDebugEnabled)
                    //    log.Debug("++++++++++++++++++Remote ongoing completed, cleaning up " + midKey);
                    _exchangesByID.Remove(midKey);
                }
            }
        }
    }
}
