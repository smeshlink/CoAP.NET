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
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// This class describes the functionality of a CoAP transfer layer. It provides:
    /// 1. Support for block-wise transfers using BLOCK1 and BLOCK2 options
    /// </summary>
    public class TransferLayer : UpperLayer
    {
        private TokenManager _tokenManager;
        private Int32 _defaultSZX;
        private Int32 _defaultBlockSize;
        private IDictionary<String, Message> _incomplete = new HashMap<String, Message>();
        private IDictionary<String, Message> _sentMessages = new HashMap<String, Message>();
        private IDictionary<String, Int32> _awaiting = new HashMap<String, Int32>();

        /// <summary>
        /// Initializes a transfer layer.
        /// </summary>
        /// <param name="tokenManager"></param>
        /// <param name="defaultBlockSize">The default block size used for block-wise transfers or -1 to disable outgoing block-wise transfers</param>
        public TransferLayer(TokenManager tokenManager, Int32 defaultBlockSize)
        {
            this._tokenManager = tokenManager;
            if (defaultBlockSize > 0)
            {
                this._defaultSZX = BlockOption.EncodeSZX(defaultBlockSize);
                if (!BlockOption.ValidSZX(this._defaultSZX))
                {
                    if (Log.IsWarningEnabled)
                        Log.Warning(this, "Unsupported block size {0}, using {1} instead", defaultBlockSize, CoapConstants.DefaultBlockSize);
                    this._defaultSZX = BlockOption.EncodeSZX(CoapConstants.DefaultBlockSize);
                }
                this._defaultBlockSize = BlockOption.DecodeSZX(this._defaultSZX);
            }
            else
            {
                this._defaultSZX = -1;
            }
        }

        /// <summary>
        /// Initializes a transfer layer.
        /// </summary>
        public TransferLayer(TokenManager tokenManager)
            : this(tokenManager, CoapConstants.DefaultBlockSize)
        { }

        /// <summary>
        /// Sending a message.
        /// </summary>
        /// <param name="msg">The message to be sent</param>
        protected override void DoSendMessage(Message msg)
        {
            int sendSZX = this._defaultSZX;
            int sendNUM = 0;

            // block size negotiation
            if (msg.IsResponse && ((Response)msg).Request != null)
            {
                BlockOption buddyBlock = (BlockOption)((Response)msg).Request.GetFirstOption(OptionType.Block2);
                if (buddyBlock != null)
                {
                    if (buddyBlock.SZX < sendSZX)
                        sendSZX = buddyBlock.SZX;
                    sendNUM = buddyBlock.NUM;
                }
            }

            // check if message needs to be split up
            if (BlockOption.ValidSZX(sendSZX) && (msg.PayloadSize > BlockOption.DecodeSZX(sendSZX)))
            {
                // split message up using block1 for requests and block2 for responses
                if (msg.RequiresToken)
                {
                    msg.Token = this._tokenManager.AcquireToken(false);
                }

                Message block = GetBlock(msg, sendNUM, sendSZX);

                if (block != null)
                {
                    // send block and wait for reply
                    SendMessageOverLowerLayer(block);

                    // store if not complete
                    BlockOption blockOpt = (BlockOption)block.GetFirstOption(OptionType.Block2);
                    if (blockOpt.M)
                    {
                        this._incomplete.Add(msg.TransferID, msg);
                        //TODO timeout to clean up incomplete Map after a while
                        if (Log.IsInfoEnabled)
                            Log.Info(this, "Transfer cached for {0}", msg.TransferID);
                    }
                    else
                    {
                        if (Log.IsInfoEnabled)
                            Log.Info(this, "Blockwise transfer complete | {0}", msg.TransferID);
                    }

                    // update timestamp
                    msg.Timestamp = block.Timestamp;
                }
                else
                {
                    HandleOutOfScopeError(msg);
                }

                //this._incomplete.Add(msg.TransferID, msg);
                //this._sentMessages.Add(msg.TransferID, msg);

                //if (Log.IsInfoEnabled)
                //    Log.Info(this, "Transfer initiated for {0}", msg.TransferID);

                //// send only first block and wait for reply
                //SendMessageOverLowerLayer(block);

                //// update timestamp
                //msg.Timestamp = block.Timestamp;
            }
            else
            {
                // TODO 消息取消时移除暂存项
                _sentMessages[msg.TransferID] = msg;

                SendMessageOverLowerLayer(msg);
            }
        }

        /// <summary>
        /// Receiving a message.
        /// </summary>
        /// <param name="msg">The message to be received</param>
        protected override void DoReceiveMessage(Message msg)
        {
            BlockOption block1 = (BlockOption)msg.GetFirstOption(OptionType.Block1);
            BlockOption block2 = (BlockOption)msg.GetFirstOption(OptionType.Block2);

            Message first = _incomplete[msg.TransferID];

            if (null == block1 && null == block2 && !msg.RequiresBlockwise)
            {
                if (first is Request && msg is Response)
                {
                    //((Response)msg).Request = (Request)first;
                    if (((Response)msg).Request == null)
                    {
                        if (Log.IsErrorEnabled)
                            Log.Error(this, "Received unmatched response | {0}", msg.Key);
                    }
                }
                _sentMessages.Remove(msg.TransferID);
                DeliverMessage(msg);
            }
            else if (msg.IsRequest && (block1 != null || msg.RequiresBlockwise))
            {
                // handle incoming payload using block1

                if (msg.RequiresBlockwise)
                {
                    if (Log.IsInfoEnabled)
                        Log.Info(this, "Requesting blockwise transfer | {0}", msg.Key);
                    if (first != null)
                    {
                        _incomplete.Remove(msg.TransferID);
                        if (Log.IsErrorEnabled)
                            Log.Error(this, "Resetting incomplete transfer | {0}", msg.Key);
                    }
                    block1 = new BlockOption(msg.IsRequest ? OptionType.Block1 : OptionType.Block2, 0, BlockOption.EncodeSZX(CoapConstants.DefaultBlockSize), true);
                }

                if (Log.IsInfoEnabled)
                    Log.Info(this, "Incoming payload, block1");

                HandleIncomingPayload(msg, block1);
            }
            else if (msg.IsRequest && block2 != null)
            {
                // send blockwise response
                if (Log.IsInfoEnabled)
                    Log.Info(this, "Block request received : {0} | {1}", block2, msg.Key);

                if (null == first)
                {
                    // get current representation
                    if (Log.IsInfoEnabled)
                        Log.Info(this, "New blockwise transfer | {0}", msg.TransferID);
                    DeliverMessage(msg);
                }
                else
                {
                    // use cached representation
                    Message resp = GetBlock(first, block2.NUM, block2.SZX);
                    if (resp != null)
                    {
                        resp.ID = msg.ID;
                        BlockOption respBlock = (BlockOption)resp.GetFirstOption(OptionType.Block2);
                        try
                        {
                            SendMessageOverLowerLayer(resp);
                            if (Log.IsInfoEnabled)
                                Log.Info(this, "Block request responded: {0} | {1}", respBlock, resp.Key);
                        }
                        catch (Exception ex)
                        {
                            if (Log.IsErrorEnabled)
                                Log.Error(this, "Failed to send block response: {0}", ex.Message);
                            throw;
                        }

                        // remove transfer context if completed
                        if (!respBlock.M)
                        {
                            this._incomplete.Remove(msg.TransferID);
                            if (Log.IsInfoEnabled)
                                Log.Info(this, "Blockwise transfer complete | {0}", resp.TransferID);
                        }
                    }
                    else
                    {
                        HandleOutOfScopeError(msg.NewReply(true));
                    }
                }
            }
            else if (msg.IsResponse && block1 != null)
            {
                // handle blockwise acknowledgement
                if (null != first)
                {
                    if (!msg.IsReset)
                    {
                        // send next block
                        Message block = GetBlock(first, block1.NUM + 1, block1.SZX);
                        try
                        {
                            SendMessageOverLowerLayer(block);
                        }
                        catch (Exception ex)
                        {
                            if (Log.IsErrorEnabled)
                                Log.Error(this, ex.Message);
                        }

                        return;
                    }
                    else
                    {
                        // cancel transfer
                        if (Log.IsInfoEnabled)
                            Log.Info(this, "Block-wise transfer cancelled by peer (RST): {0}", msg.TransferID);
                        this._incomplete.Remove(msg.TransferID);

                        DeliverMessage(msg);
                    }
                }
                else
                {
                    if (Log.IsWarningEnabled)
                        Log.Warning(this, "Unexpected reply in blockwise transfer dropped: {0}", msg.Key);
                }
            }
            else if (msg.IsResponse && block2 != null)
            {
                if (Log.IsInfoEnabled)
                    Log.Info(this, "Incoming payload, block2");
                // handle incoming payload using block2
                HandleIncomingPayload(msg, block2);
            }
        }

        private void HandleIncomingPayload(Message msg, BlockOption blockOpt)
        {
            if (msg is Response)
            {
                Response response = (Response)msg;
                if (null == response.Request && this._sentMessages.ContainsKey(msg.TransferID))
                    response.Request = this._sentMessages[msg.TransferID] as Request;
            }

            Message initial = _incomplete[msg.TransferID];

            if (initial != null)
            {
                int awaitNUM = -1;
                if (_awaiting.ContainsKey(msg.TransferID))
                    awaitNUM = _awaiting[msg.TransferID];
                // compare block offsets
                // FIXME block1 or block2?
                //if (blockOpt.NUM == awaitNUM)
                //if (blockOpt.NUM * blockOpt.Size == awaitNUM * ((BlockOption)initial.GetFirstOption(OptionType.Block1)).Size)
                if (blockOpt.NUM * blockOpt.Size == awaitNUM * ((BlockOption)initial.GetFirstOption(OptionType.Block2)).Size)
                {
                    // append received payload to first response
                    initial.AppendPayload(msg.Payload);
                    _awaiting[msg.TransferID] = blockOpt.NUM + 1;

                    // update info
                    initial.ID = msg.ID;
                    initial.SetOption(blockOpt);

                    if (Log.IsInfoEnabled)
                        Log.Info(this, "Block received : {0}", blockOpt);
                }
                else
                {
                    if (Log.IsWarningEnabled)
                        Log.Warning(this, "Wrong block received : {0}", blockOpt);
                }
            }
            else if (blockOpt.NUM == 0 && msg.PayloadSize > 0)
            {
                // calculate next block num from received payload length
                Int32 size = blockOpt.Size;
                Int32 num = (msg.PayloadSize / size) - 1;
                blockOpt.NUM = num;
                msg.SetOption(blockOpt);

                // crop payload
                Byte[] newPayload = new Byte[(num + 1) * size];
                Array.Copy(msg.Payload, 0, newPayload, 0, newPayload.Length);
                msg.Payload = newPayload;

                // create new transfer context
                initial = msg;
                _incomplete[msg.TransferID] = initial;
                _awaiting[msg.TransferID] = blockOpt.NUM + 1;

                if (Log.IsInfoEnabled)
                    Log.Info(this, "Transfer initiated for {0}", msg.TransferID);
            }
            else
            {
                if (Log.IsErrorEnabled)
                    Log.Error(this, "Transfer started out of order: {0}", msg.Key);
                HandleIncompleteError(msg.NewReply(true));
                return;
            }

            if (initial is Response && (initial as Response).Request.Canceled)
            {
                // transfer cancelled
                this._incomplete.Remove(msg.TransferID);
                this._sentMessages.Remove(msg.TransferID);
                return;
            }

            if (blockOpt.M)
            {
                Message reply = null;
                if (msg is Response)
                {
                    // more data available
                    // request next block
                    reply = Split(((Response)msg).Request, blockOpt.NUM + 1, blockOpt.SZX);

                    //reply = new Request(Code.GET, msg.IsConfirmable);
                    //reply.URI = msg.URI;

                    //// TODO set Accept

                    //reply.SetOption(msg.GetFirstOption(OptionType.Token));
                    //reply.RequiresToken = msg.RequiresToken;
                    ////reply.SetOption(new BlockOption(OptionType.Block2, blockOpt.NUM + 1, blockOpt.SZX, false));
                    //reply.SetOption(new BlockOption(OptionType.Block2, _awaiting[msg.TransferID], blockOpt.SZX, false));
                }
                else if (msg is Request)
                {
                    reply = msg.NewReply(true);

                    // picked arbitrarily, cannot decide if created or changed without putting resource logic here
                    reply.Code = Code.Created;

                    // echo block option
                    reply.AddOption(blockOpt);
                }
                else
                {
                    if (Log.IsErrorEnabled)
                        Log.Error(this, "Unsupported message type: {0}", msg.Key);
                    return;
                }

                try
                {
                    SendMessageOverLowerLayer(reply);

                    if (Log.IsInfoEnabled)
                    {
                        BlockOption replyBlock = (BlockOption)reply.GetFirstOption(blockOpt.Type);
                        Log.Info(this, "Block replied: {0}, {1}", reply.Key, replyBlock);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.IsErrorEnabled)
                        Log.Error(this, "Failed to request block: {0}", ex.Message);
                }
            }
            else
            {
                DeliverMessage(initial);
                _incomplete.Remove(msg.TransferID);
                _sentMessages.Remove(msg.TransferID);
                _awaiting.Remove(msg.TransferID);

                // transfer complete
                if (Log.IsInfoEnabled)
                    Log.Info(this, "Transfer completed: {0}", msg.TransferID);
            }
        }

        private static Message Split(Message msg, Int32 num, Int32 szx)
        {
            Message block = null;
            // TODO 使用更优雅的方式，如msg.New()/Clone()
            block = (Message)msg.GetType().Assembly.CreateInstance(msg.GetType().FullName);

            block.Type = msg.Type;
            block.Code = msg.Code;
            block.URI = msg.URI;

            // TODO set options (Content-Type, Max-Age etc)	

            block.SetOption(msg.GetFirstOption(OptionType.Token));
            block.RequiresToken = msg.RequiresToken;
            block.SetOption(new BlockOption(OptionType.Block2, num, szx, false));

            return block;
        }

        private void HandleOutOfScopeError(Message resp)
        {
            resp.Code = Code.BadRequest;
            resp.PayloadString = "BlockOutOfScope";
            try
            {
                SendMessageOverLowerLayer(resp);

                if (Log.IsInfoEnabled)
                    Log.Info(this, "Out-of-scope block request rejected | {0}", resp.Key);
            }
            catch (Exception ex)
            {
                if (Log.IsErrorEnabled)
                    Log.Error(this, "Failed to send error message: {0}", ex.Message);
            }
        }

        private void HandleIncompleteError(Message resp)
        {
            resp.Code = Code.RequestEntityIncomplete;
            resp.PayloadString = "Start with block num 0";
            try
            {
                SendMessageOverLowerLayer(resp);

                if (Log.IsInfoEnabled)
                    Log.Info(this, "Incomplete request rejected | {0}", resp.Key);
            }
            catch (Exception ex)
            {
                if (Log.IsErrorEnabled)
                    Log.Error(this, "Failed to send error message: {0}", ex.Message);
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
                // TODO 使用更优雅的方式，如msg.New()/Clone()
                block = (Message)msg.GetType().Assembly.CreateInstance(msg.GetType().FullName);

                block.ID = msg.ID;
                block.Type = msg.Type;
                block.Code = msg.Code;

                // use same options
                foreach (Option opt in msg.GetOptionList())
                {
                    block.AddOption(opt);
                }

                block.URI = msg.URI;
                block.RequiresToken = msg.RequiresToken;

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
                if (msg.IsRequest)
                {
                    blockOpt = new BlockOption(OptionType.Block1, num, szx, m);
                }
                else
                {
                    blockOpt = new BlockOption(OptionType.Block2, num, szx, m);
                }
                block.SetOption(blockOpt);

                return block;
            }
            else
            {
                return null;
            }
        }
    }
}
