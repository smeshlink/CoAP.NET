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
using CoAP.Log;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// The class TransferLayer provides support for blockwise transfers.
    /// </summary>
    public class TransferLayer : UpperLayer
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(TransferLayer));

        private HashMap<String, TransferContext> _incoming = new HashMap<String, TransferContext>();
        private HashMap<String, TransferContext> _outgoing = new HashMap<String, TransferContext>();
        private Int32 _defaultSZX;

        /// <summary>
        /// Initializes a transfer layer.
        /// </summary>
        /// <param name="defaultBlockSize">The default block size used for block-wise transfers or -1 to disable outgoing block-wise transfers</param>
        public TransferLayer(Int32 defaultBlockSize)
        {
            if (defaultBlockSize == 0)
                defaultBlockSize = CoapConstants.DefaultBlockSize;

            if (defaultBlockSize > 0)
            {
                _defaultSZX = BlockOption.EncodeSZX(defaultBlockSize);
                if (!BlockOption.ValidSZX(_defaultSZX))
                {
                    _defaultSZX = defaultBlockSize > 1024 ? 6 : BlockOption.EncodeSZX(defaultBlockSize & 0x07f0);
                    if (log.IsWarnEnabled)
                        log.Warn(String.Format("TransferLayer - Unsupported block size {0}, using {1} instead", defaultBlockSize, BlockOption.DecodeSZX(_defaultSZX)));
                }
            }
            else
            {
                // disable outgoing blockwise transfers
                _defaultSZX = -1;
            }
        }

        /// <summary>
        /// Initializes a transfer layer.
        /// </summary>
        public TransferLayer()
            : this(0)
        { }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            int sendSZX = _defaultSZX;
            int sendNUM = 0;

            // block negotiation
            if ((msg is Response) && ((Response)msg).Request != null)
            {
                BlockOption buddyBlock = (BlockOption)((Response)msg).Request.GetFirstOption(OptionType.Block2);
                if (buddyBlock != null)
                {
                    if (buddyBlock.SZX < sendSZX)
                        sendSZX = buddyBlock.SZX;
                    sendNUM = buddyBlock.NUM;
                }
            }

            // check if transfer needs to be split up
            if (msg.PayloadSize > BlockOption.DecodeSZX(sendSZX))
            {
                // split message up using block1 for requests and block2 for responses
                Message msgBlock = GetBlock(msg, sendNUM, sendSZX);

                if (msgBlock != null)
                {
                    BlockOption block1 = (BlockOption)msgBlock.GetFirstOption(OptionType.Block1);
                    BlockOption block2 = (BlockOption)msgBlock.GetFirstOption(OptionType.Block2);

                    // only cache if blocks remaining for request
                    if ((block1 != null && block1.M) || (block2 != null && block2.M))
                    {
                        msg.SetOption(block1);
                        msg.SetOption(block2);

                        TransferContext transfer = new TransferContext(msg);
                        _outgoing[msg.SequenceKey] = transfer;

                        if (log.IsDebugEnabled)
                            log.Debug(String.Format("TransferLayer - Caching blockwise transfer for NUM {0} : {1}", sendNUM, msg.SequenceKey));
                    }
                    else
                    {
                        // must be block2 by client
                        if (log.IsDebugEnabled)
                            log.Debug(String.Format("TransferLayer - Answering block request without caching: {0} | {1}", msg.SequenceKey, block2));
                    }

                    // send block and wait for reply
                    SendMessageOverLowerLayer(msgBlock);
                }
                else
                {
                    // must be block2 by client
                    if (log.IsInfoEnabled)
                        log.Info(String.Format("TransferLayer - Rejecting initial out-of-scope request: {0} | NUM: {1}, SZX: {2} ({3} bytes), M: n/a, {4} bytes available",
                            msg.SequenceKey, sendNUM, sendSZX, BlockOption.DecodeSZX(sendSZX), msg.PayloadSize));
                    HandleOutOfScopeError(msg.NewReply(Code.BadRequest, true));
                }
            }
            else
            {
                // send complete message
                SendMessageOverLowerLayer(msg);
            }
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            BlockOption blockIn = null, blockOut = null;

            if (msg is Request)
            {
                blockIn = (BlockOption)msg.GetFirstOption(OptionType.Block1);
                blockOut = (BlockOption)msg.GetFirstOption(OptionType.Block2);
            }
            else if (msg is Response)
            {
                blockIn = (BlockOption)msg.GetFirstOption(OptionType.Block2);
                blockOut = (BlockOption)msg.GetFirstOption(OptionType.Block1);
                if (blockOut != null)
                    blockOut.NUM++;
            }
            else if (log.IsWarnEnabled)
            {
                log.Warn("TransferLayer - Unknown message type received: " + msg.Key);
                return;
            }

            if (blockIn == null && msg.RequiresBlockwise)
            {
                // message did not have Block option, but was marked for blockwise transfer
                blockIn = new BlockOption(OptionType.Block1, 0, _defaultSZX, true);
                HandleIncomingPayload(msg, blockIn);
                return;
            }
            else if (blockIn != null)
            {
                HandleIncomingPayload(msg, blockIn);
                return;
            }
            else if (blockOut != null)
            {
                if (log.IsDebugEnabled)
                    log.Debug(String.Format("TransferLayer - Received demand for next block: {0} | {1}", msg.SequenceKey, blockOut));

                TransferContext transfer = _outgoing[msg.SequenceKey];
                if (transfer != null)
                {
                    Request req = msg as Request;
                    if (req != null && (!req.UriPath.Equals(transfer.uriPath) || !req.UriQuery.Equals(transfer.uriQuery)))
                    {
                        _outgoing.Remove(msg.SequenceKey);
                        if (log.IsDebugEnabled)
                            log.Debug("TransferLayer - Freed blockwise transfer by client token reuse: " + msg.SequenceKey);
                    }
                    else
                    {
                        if (req != null)
                            UpdateCache(transfer, msg);

                        // use cached representation
                        Message next = GetBlock(transfer.cache, blockOut.NUM, blockOut.SZX);
                        if (next != null)
                        {
                            try
                            {
                                if (log.IsDebugEnabled)
                                    log.Debug(String.Format("TransferLayer - Sending next block: {0} | {1}", next.SequenceKey, blockOut));
                                SendMessageOverLowerLayer(next);
                            }
                            catch (Exception ex)
                            {
                                if (log.IsErrorEnabled)
                                    log.Error("TransferLayer - Failed to send block response: " + ex.Message);
                            }

                            BlockOption respBlock = (BlockOption)next.GetFirstOption(blockOut.Type);
                            // remove transfer context if completed
                            if (!respBlock.M && msg is Request)
                            {
                                _outgoing.Remove(msg.SequenceKey);
                                if (log.IsDebugEnabled)
                                    log.Debug("TransferLayer - Freed blockwise download by completion: " + next.SequenceKey);
                            }
                            return;
                        }
                        else if (msg is Response && !blockOut.M)
                        {
                            _outgoing.Remove(msg.SequenceKey);
                            if (log.IsDebugEnabled)
                                log.Debug("TransferLayer - Freed blockwise upload by completion: " + msg.SequenceKey);
                            // restore original request with registered handlers
                            ((Response)msg).Request = (Request)transfer.cache;
                        }
                        else
                        {
                            if (log.IsWarnEnabled)
                                log.Warn(String.Format("TransferLayer - Rejecting out-of-scope demand for cached transfer (freed): {0} | {1}, {2} bytes available",
                                    msg.SequenceKey, blockOut, transfer.cache.PayloadSize));
                            _outgoing.Remove(msg.SequenceKey);
                            HandleOutOfScopeError(msg.NewReply(Code.BadRequest, true));
                            return;
                        }
                    }
                }
            }
            else if (msg is Response)
            {
                // check for cached transfers
                TransferContext transfer = _outgoing[msg.SequenceKey];
                if (transfer != null)
                {
                    // restore original request with registered handlers
                    ((Response)msg).Request = (Request)transfer.cache;
                    _outgoing.Remove(msg.SequenceKey);
                    if (log.IsDebugEnabled)
                        log.Debug("TransferLayer - Freed outgoing transfer by client abort: " + msg.SequenceKey);
                }

                transfer = _incoming[msg.SequenceKey];
                if (transfer != null)
                {
                    // restore original request with registered handlers
                    ((Response)msg).Request = (Request)transfer.cache;
                    _incoming.Remove(msg.SequenceKey);
                    if (log.IsDebugEnabled)
                        log.Debug("TransferLayer - Freed imcoming transfer by client abort: " + msg.SequenceKey);
                }
            }

            // get current representation/deliver response
            DeliverMessage(msg);
        }

        private void UpdateCache(TransferContext transfer, Message msg)
        {
            transfer.cache.ID = msg.ID;
            //transfer.cache.SetOptions(msg.GetOptions(OptionType.Block1));
            //transfer.cache.SetOptions(msg.GetOptions(OptionType.Block2));
        }

        private void HandleIncomingPayload(Message msg, BlockOption blockOpt)
        {
            TransferContext transfer = _incoming[msg.SequenceKey];
            
            if (transfer != null && blockOpt.NUM > 0)
            {
                // compare block offsets
                if (blockOpt.NUM * blockOpt.Size == (transfer.current.NUM + 1) * transfer.current.Size)
                {
                    // append received payload to first response and update message ID
                    transfer.cache.AppendPayload(msg.Payload);
                    UpdateCache(transfer, msg);
                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("TransferLayer - Received next block: {0} | {1}", msg.SequenceKey, blockOpt));
                }
                else if (log.IsDebugEnabled)
                    log.Debug(String.Format("TransferLayer - Dropping wrong block: {0} | {1}", msg.SequenceKey, blockOpt));
            }
            else if (blockOpt.NUM == 0 && (msg.PayloadSize == blockOpt.Size || !blockOpt.M))
            {
                // configure messages for blockwise transfer
                if (msg.PayloadSize > blockOpt.Size)
                {
                    Int32 newNum = msg.PayloadSize / blockOpt.Size;
                    blockOpt.NUM = newNum - 1;
                    Byte[] bytes = new Byte[newNum];
                    Array.Copy(msg.Payload, bytes, newNum);
                    msg.Payload = bytes;
                }

                // create new transfer context
                transfer = new TransferContext(msg);
                _incoming[msg.SequenceKey] = transfer;

                if (log.IsDebugEnabled)
                    log.Debug(String.Format("TransferLayer - Incoming blockwise transfer: {0} | {1}", msg.SequenceKey, blockOpt));
            }
            else
            {
                if (log.IsDebugEnabled)
                    log.Debug(String.Format("TransferLayer - Rejecting out-of-order block: {0} | {1}", msg.SequenceKey, blockOpt));
                HandleIncompleteError(msg.NewReply(Code.RequestEntityIncomplete, true));
                return;
            }

            if (transfer.cache is Response && ((Response)transfer.cache).Request.Canceled)
            {
                // transfer cancelled
                if (log.IsDebugEnabled)
                    log.Debug("TransferLayer - Canceling transfer: " + msg.Key);
                _incoming.Remove(msg.SequenceKey);
                return;
            }

            if (blockOpt.M)
            {
                Message reply = null;

                Int32 demandSZX = blockOpt.SZX;
                Int32 demandNUM = blockOpt.NUM;

                // block size negotiation
                if (demandSZX > _defaultSZX)
                {
                    demandNUM = demandSZX / _defaultSZX * demandNUM;
                    demandSZX = _defaultSZX;
                }

                if (msg is Response)
                {
                    Request request = new Request(Code.GET, !msg.IsNonConfirmable); // msg could be ACK or CON
                    request.PeerAddress = msg.PeerAddress;
                    request.UriPath = transfer.uriPath;
                    request.UriQuery = transfer.uriQuery;
                    reply = request;

                    // get next block
                    demandNUM++;
                }
                else if (msg is Request)
                {
                    // picked arbitrary code, cannot decide if created or changed without putting resource logic here
                    reply = new Response(Code.Changed);
                    reply.Type = msg.IsConfirmable ? MessageType.ACK : MessageType.NON;
                    reply.PeerAddress = msg.PeerAddress;
                    if (msg.IsConfirmable)
                        reply.ID = msg.ID;
                    
                    // increase NUM for next block after ACK
                }
                else
                {
                    if (log.IsErrorEnabled)
                        log.Error("TransferLayer - Unsupported message type: " + msg.Key);
                    return;
                }

                // MORE=1 for Block1, as CoAP.NET handles transfers atomically
                BlockOption next = new BlockOption(blockOpt.Type, demandNUM, demandSZX, blockOpt.Type == OptionType.Block1);
                reply.SetOption(next);
                // echo options
                reply.Token = msg.Token;

                try
                {
                    if (log.IsDebugEnabled)
                        log.Debug(String.Format("TransferLayer - Demanding next block: {0} | {1}", reply.SequenceKey, next));
                    SendMessageOverLowerLayer(reply);
                }
                catch (Exception ex)
                {
                    if (log.IsErrorEnabled)
                        log.Error("TransferLayer - Failed to request block: " + ex.Message);
                }

                // update incoming transfer
                transfer.current = blockOpt;
            }
            else
            {
                // set final block option
                transfer.cache.SetOption(blockOpt);
                if (log.IsDebugEnabled)
                    log.Debug("TransferLayer - Finished blockwise transfer: " + msg.SequenceKey);
                _incoming.Remove(msg.SequenceKey);
                DeliverMessage(transfer.cache);
            }
        }

        private void HandleOutOfScopeError(Message resp)
        {
            resp.PayloadString = "BlockOutOfScope";
            try
            {
                SendMessageOverLowerLayer(resp);
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error("TransferLayer - Failed to send error message: " + ex.Message);
            }
        }

        private void HandleIncompleteError(Message resp)
        {
            resp.PayloadString = "Start with block num 0";
            try
            {
                SendMessageOverLowerLayer(resp);
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error("TransferLayer - Failed to send error message: " + ex.Message);
            }
        }

        private static Message GetBlock(Message msg, Int32 num, Int32 szx)
        {
            Int32 blockSize = BlockOption.DecodeSZX(szx);
            Int32 payloadOffset = num * blockSize;
            Int32 payloadLeft = msg.PayloadSize - payloadOffset;

            if (payloadLeft > 0)
            {
                Message block = null;
                if (msg is Request)
                    block = new Request(msg.Code, msg.IsConfirmable);
                else
                {
                    block = new Response(msg.Code);
                    if (num == 0 && msg.Type == MessageType.CON)
                        block.Type = MessageType.CON;
                    else
                        block.Type = msg.IsNonConfirmable ? MessageType.NON : MessageType.ACK;
                    block.ID = msg.ID;
                }

                block.PeerAddress = msg.PeerAddress;
                block.Token = msg.Token;

                // use same options
                foreach (Option opt in msg.GetOptions())
                {
                    block.AddOption(opt);
                }

                // calculate 'more' bit 
                Boolean m = blockSize < payloadLeft;
                // limit block size to size of payload left
                if (!m)
                {
                    blockSize = payloadLeft;
                }

                // copy payload block
                Byte[] blockPayload = new Byte[blockSize];
                Array.Copy(msg.Payload, payloadOffset, blockPayload, 0, blockSize);
                block.Payload = blockPayload;

                Option blockOpt = null;
                if (msg is Request)
                    blockOpt = new BlockOption(OptionType.Block1, num, szx, m);
                else
                    blockOpt = new BlockOption(OptionType.Block2, num, szx, m);
                block.SetOption(blockOpt);

                return block;
            }
            else
            {
                return null;
            }
        }

        class TransferContext
        {
            public Message cache;
            public String uriPath;
            public String uriQuery;
            public BlockOption current;

            public TransferContext(Message msg)
            {
                if (msg is Request)
                {
                    Request request = msg as Request;
                    cache = request;
                    uriPath = request.UriPath;
                    uriQuery = request.UriQuery;
                    current = (BlockOption)msg.GetFirstOption(OptionType.Block1);
                }
                else if (msg is Response)
                {
                    msg.RequiresToken = false;
                    cache = msg;
                    uriPath = ((Response)msg).Request.UriPath;
                    uriQuery = ((Response)msg).Request.UriQuery;
                    current = (BlockOption)msg.GetFirstOption(OptionType.Block2);
                }

                if (log.IsDebugEnabled)
                    log.Debug(String.Format("TransferLayer - Created new transfer context for {0}?{1}: {2}",
                        uriPath, uriQuery, msg.SequenceKey));
            }
        }
    }
}
