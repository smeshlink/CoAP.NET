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
using System.Net;
using System.Text;
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// This class describes the functionality of the CoAP messages
    /// </summary>
    public class Message
    {
        const Int32 VersionBits = 2;
        const Int32 TypeBits = 2;
        const Int32 OptionCountBits = 4;
        const Int32 CodeBits = 8;
        const Int32 IDBits = 16;
        const Int32 OptionDeltaBits = 4;
        const Int32 OptionLengthBaseBits = 4;
        const Int32 OptionLengthExtendedBits = 8;
        public const Int32 MaxOptionDelta = (1 << OptionDeltaBits) - 1;
        public const Int32 MaxOptionLengthBase = (1 << OptionLengthBaseBits) - 2;
        public const Int32 MaxID = (1 << IDBits) - 1;
        public const Int32 InvalidID = -1;

        private Int32 _version = 1;
        private MessageType _type;
        private Int32 _code;
        private Int32 _id = InvalidID;
        private Byte[] _payLoadBytes;
        private Message _buddy;
        protected Boolean _requiresToken = true;
        protected Boolean _requiresBlockwise = false;
        private IDictionary<OptionType, IList<Option>> _optionMap = new SortedDictionary<OptionType, IList<Option>>();
        private Int64 _timestamp;
        private Uri _uri;
        private Boolean _cancelled = false;
        private Boolean _complete = false;

        /// <summary>
        /// Initializes a message.
        /// </summary>
        public Message()
        {
        }

        /// <summary>
        /// Initializes a message.
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="code">The message code</param>
        public Message(MessageType type, Int32 code)
        {
            this._type = type;
            this._code = code;
        }

        /// <summary>
        /// Initializes a message.
        /// </summary>
        public Message(Uri uri, MessageType type, Int32 code, Int32 id, Byte[] payload)
        {
            _uri = uri;
            _type = type;
            _code = code;
            _id = id;
            _payLoadBytes = payload;
        }

        /// <summary>
        /// Creates a reply message to this message.
        /// </summary>
        /// <param name="ack">Acknowledgement or not</param>
        /// <returns></returns>
        public Message NewReply(Boolean ack)
        {
            Message reply = new Message();

            if (this._type == MessageType.CON)
            {
                reply._type = ack ? MessageType.ACK : MessageType.RST;
            }
            else
            {
                reply._type = MessageType.NON;
            }

            // echo ID
            reply._id = this._id;
            // set the receiver URI of the reply to the sender of this message
            reply.URI = this.URI;

            // echo token
            reply.SetOption(GetFirstOption(OptionType.Token));
            reply.RequiresToken = this.RequiresToken;
            // create an empty reply by default
            reply.Code = CoAP.Code.Empty;

            return reply;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        public void HandleBy(IMessageHandler handler)
        {
            DoHandleBy(handler);
        }

        /// <summary>
        /// Notification method that is called when the transmission of this
        /// message was canceled due to timeout.
        /// </summary>
        public void HandleTimeout()
        {
            DoHandleTimeout();
        }

        /// <summary>
        /// Appends data to this message's payload.
        /// </summary>
        /// <param name="block">The byte array to be appended</param>
        public void AppendPayload(Byte[] block)
        {
            if (null != block)
            {
                System.Threading.Monitor.Enter(this);
                if (null == this._payLoadBytes)
                {
                    this._payLoadBytes = (Byte[])block.Clone();
                }
                else
                {
                    Byte[] newPayload = new Byte[this._payLoadBytes.Length + block.Length];
                    Array.Copy(this._payLoadBytes, 0, newPayload, 0, this._payLoadBytes.Length);
                    Array.Copy(block, 0, newPayload, this._payLoadBytes.Length, block.Length);
                    this._payLoadBytes = newPayload;
                }
                System.Threading.Monitor.PulseAll(this);
                // TODO add event
                //PayloadAppended(block);
                System.Threading.Monitor.Exit(this);
            }
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        public void SetPayload(String payload)
        {
            //SetPayload(payload, MediaType.TextPlain);
            SetPayload(payload, MediaType.Undefined);
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        /// <param name="mediaType">The content-type of the payload</param>
        public void SetPayload(String payload, Int32 mediaType)
        {
            if (!String.IsNullOrEmpty(payload))
            {
                // TODO UTF8?
                Payload = System.Text.Encoding.UTF8.GetBytes(payload);
                if (mediaType != MediaType.Undefined)
                    SetOption(Option.Create(OptionType.ContentType, mediaType));
            }
        }

        /// <summary>
        /// Encodes the message into its raw binary representation.
        /// </summary>
        /// <returns>A byte array containing the CoAP encoding of the message</returns>
        public Byte[] Encode()
        {
            // create datagram writer to encode options
            DatagramWriter optWriter = new DatagramWriter();
            Int32 optionCount = 0;
            Int32 lastOptionNumber = 0;
            foreach (Option opt in GetOptionList())
            {
                if (opt.IsDefault)
                    continue;

                Int32 optionDelta = (Int32)opt.Type - lastOptionNumber;

                // ensure that option delta value can be encoded correctly
                while (optionDelta > MaxOptionDelta)
                {
                    // option delta is too large to be encoded:
                    // add fencepost options in order to reduce the option delta
                    // get fencepost option that is next to the last option
                    Int32 fencepostNumber =
                        Option.NextFencepost(lastOptionNumber);

                    // calculate fencepost delta
                    int fencepostDelta = fencepostNumber - lastOptionNumber;
                    if (fencepostDelta <= 0)
                    {
                        if (Log.IsWarningEnabled)
                            Log.Warning(this, "Fencepost liveness violated: delta = {0}\n", fencepostDelta);
                    }
                    if (fencepostDelta > MaxOptionDelta)
                    {
                        if (Log.IsWarningEnabled)
                            Log.Warning(this, "Fencepost safety violated: delta = {0}\n", fencepostDelta);
                    }

                    // write fencepost option delta
                    optWriter.Write(fencepostDelta, OptionDeltaBits);
                    // fencepost have an empty value
                    optWriter.Write(0, OptionLengthBaseBits);

                    ++optionCount;
                    lastOptionNumber = fencepostNumber;
                    optionDelta -= fencepostDelta;
                }

                // write option delta
                optWriter.Write(optionDelta, OptionDeltaBits);

                // write option length
                Int32 length = opt.Length;
                if (length <= MaxOptionLengthBase)
                {
                    // use option length base field only to encode
                    // option lengths less or equal than MAX_OPTIONLENGTH_BASE
                    optWriter.Write(length, OptionLengthBaseBits);
                }
                else
                {
                    // use both option length base and extended field
                    // to encode option lengths greater than MAX_OPTIONLENGTH_BASE
                    Int32 baseLength = MaxOptionLengthBase + 1;
                    optWriter.Write(baseLength, OptionLengthBaseBits);

                    Int32 extLength = length - baseLength;
                    optWriter.Write(extLength, OptionLengthExtendedBits);
                }

                // write option value
                optWriter.WriteBytes(opt.RawValue);

                ++optionCount;
                lastOptionNumber = (Int32)opt.Type;
            }

            // create datagram writer to encode message data
            DatagramWriter writer = new DatagramWriter();

            // write fixed-size CoAP headers
            writer.Write(_version, VersionBits);
            writer.Write((Int32)_type, TypeBits);
            writer.Write(optionCount, OptionCountBits);
            writer.Write(_code, CodeBits);
            writer.Write(_id, IDBits);

            // write options
            writer.WriteBytes(optWriter.ToByteArray());

            //write payload
            writer.WriteBytes(_payLoadBytes);

            return writer.ToByteArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            String kind = "MESSAGE";
            if (this.IsRequest)
                kind = "REQUEST";
            else if (this.IsResponse)
                kind = "RESPONSE";
            builder.AppendFormat("==[ COAP {0} ]============================================\n", kind);

            IList<Option> options = GetOptionList();
            builder.AppendFormat("URI    :  {0}\n", null == this._uri ? "NULL" : this._uri.ToString());
            builder.AppendFormat("ID     :  {0}\n", this._id);
            builder.AppendFormat("Type   :  {0}\n", this.Type);
            builder.AppendFormat("Code   :  {0}\n", CoAP.Code.ToString(this._code));
            builder.AppendFormat("Options:  {0}\n", options.Count);
            foreach (Option opt in options)
            {
                builder.AppendFormat("  * {0}: {1} ({2} Bytes)\n", opt.Name, opt, opt.Length);
            }
            builder.AppendFormat("Payload: {0} Bytes\n", this.PayloadSize);
            builder.AppendLine("---------------------------------------------------------------");
            if (this.PayloadSize > 0)
                builder.AppendLine(this.PayloadString);
            builder.AppendLine("===============================================================");

            return builder.ToString();
        }

        #region Option operations

        /// <summary>
        /// Adds an option to the list of options of this CoAP message.
        /// </summary>
        /// <param name="opt">The option to be added</param>
        public void AddOption(Option opt)
        {
            IList<Option> list = GetOptions(opt.Type);
            if (null == list)
            {
                SetOption(opt);
            }
            else
            {
                list.Add(opt);
            }
        }

        /// <summary>
        /// Removes all options of the given type from this CoAP message.
        /// </summary>
        /// <param name="optionType">The type of option to be removed</param>
        public void RemoveOption(OptionType optionType)
        {
            this._optionMap.Remove(optionType);
        }

        /// <summary>
        /// Gets all options of the given type.
        /// </summary>
        /// <param name="optionType">The option type</param>
        /// <returns></returns>
        public IList<Option> GetOptions(OptionType optionType)
        {
            return this._optionMap.ContainsKey(optionType) ? this._optionMap[optionType] : null;
        }

        /// <summary>
        /// Sets all options with the specified option type.
        /// </summary>
        /// <param name="optionType">The option type</param>
        /// <param name="opts">The list of options</param>
        public void SetOptions(OptionType optionType, IList<Option> opts)
        {
            // TODO Check if all options are consistent with optionNumber
            this._optionMap[optionType] = opts;
            if (optionType == OptionType.Token)
            {
                this._requiresToken = false;
            }
        }

        /// <summary>
        /// Sets an option.
        /// </summary>
        /// <param name="opt">The option to be set</param>
        public void SetOption(Option opt)
        {
            if (null != opt)
            {
                IList<Option> opts = new List<Option>();
                opts.Add(opt);
                SetOptions(opt.Type, opts);
            }
        }

        /// <summary>
        /// Checks if this CoAP message has options of the specified option type.
        /// </summary>
        /// <param name="type">The option type</param>
        /// <returns>True iff options of the specified type exist</returns>
        public Boolean HasOption(OptionType type)
        {
            return GetFirstOption(type) != null;
        }

        /// <summary>
        /// Gets the first option of the specified option type.
        /// </summary>
        /// <param name="optionType">The option type</param>
        /// <returns>The first option of the specified type, or null</returns>
        public Option GetFirstOption(OptionType optionType)
        {
            IList<Option> list = GetOptions(optionType);
            return (null != list && list.Count > 0) ? list[0] : null;
        }

        /// <summary>
        /// Gets a sorted list of all options.
        /// </summary>
        /// <returns></returns>
        public IList<Option> GetOptionList()
        {
            List<Option> list = new List<Option>();
            foreach (IList<Option> opts in this._optionMap.Values)
            {
                if (null != opts)
                    list.AddRange(opts);
            }
            return list;
        }

        /// <summary>
        /// Gets the number of all options of this CoAP message.
        /// </summary>
        /// <returns></returns>
        public Int32 GetOptionCount()
        {
            return GetOptionList().Count;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size of the payload of this CoAP message.
        /// </summary>
        public Int32 PayloadSize
        {
            get { return (null == this._payLoadBytes) ? 0 : this._payLoadBytes.Length; }
        }

        /// <summary>
        /// Gets or sets the payload of this CoAP message.
        /// </summary>
        public Byte[] Payload
        {
            get { return this._payLoadBytes; }
            set { this._payLoadBytes = value; }
        }

        /// <summary>
        /// Gets or sets the payload of this CoAP message in string representation.
        /// </summary>
        public String PayloadString
        {
            get { return (null == this._payLoadBytes) ? null : System.Text.Encoding.UTF8.GetString(this._payLoadBytes); }
            set { SetPayload(value, MediaType.TextPlain); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a generated token is needed.
        /// </summary>
        public Boolean RequiresToken
        {
            get { return this._requiresToken; }
            set { this._requiresToken = value; }
        }

        public Boolean RequiresBlockwise
        {
            get { return this._requiresBlockwise; }
            set { this._requiresBlockwise = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp related to this CoAP message.
        /// </summary>
        public Int64 Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        /// Gets or sets the URI of this CoAP message.
        /// </summary>
        public Uri URI
        {
            get { return this._uri; }
            set
            {
                if (null != value)
                {
                    String path = value.AbsolutePath;
                    if (!String.IsNullOrEmpty(path))
                    {
                        IList<Option> uriPaths = Option.Split(OptionType.UriPath, path, "/");
                        SetOptions(OptionType.UriPath, uriPaths);
                    }

                    String query = value.Query;
                    if (!String.IsNullOrEmpty(query))
                    {
                        IList<Option> uriQuery = Option.Split(OptionType.UriQuery, query, "&");
                        SetOptions(OptionType.UriQuery, uriQuery);
                    }
                }
                this._uri = value;
            }
        }

        /// <summary>
        /// Gets or sets the token option of this CoAP message.
        /// </summary>
        public Option Token
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Token);
                // TODO 应该如实返回吧？
                //return (null == opt) ? TokenManager.EmptyToken : opt;
                return opt;
            }
            set
            {
                SetOption(value);
            }
        }

        /// <summary>
        /// Gets or sets the content-type of this CoAP message.
        /// </summary>
        public Int32 ContentType
        {
            get
            {
                Option opt = GetFirstOption(OptionType.ContentType);
                return (null == opt) ? MediaType.Undefined : opt.IntValue;
            }
            set
            {
                if (value == MediaType.Undefined)
                {
                    SetOptions(OptionType.ContentType, null);
                }
                else
                {
                    SetOption(Option.Create(OptionType.ContentType, value));
                }
            }
        }

        /// <summary>
        /// Gets or set the location-path of this CoAP message.
        /// </summary>
        public String LocationPath
        {
            get
            {
                return Option.Join(GetOptions(OptionType.LocationPath), "/");
            }
            set
            {
                SetOptions(OptionType.LocationPath, Option.Split(OptionType.LocationPath, value, "/"));
            }
        }

        /// <summary>
        /// Gets or sets the code of this CoAP message.
        /// </summary>
        public Int32 Code
        {
            get { return this._code; }
            set { this._code = value; }
        }

        /// <summary>
        /// Gets the code's string representation of this CoAP message.
        /// </summary>
        public String CodeString
        {
            get { return CoAP.Code.ToString(this._code); }
        }

        /// <summary>
        /// Gets the version of this CoAP message.
        /// </summary>
        public Int32 Version
        {
            get { return _version; }
        }

        /// <summary>
        /// Gets or sets the ID of this CoAP message.
        /// </summary>
        public Int32 ID
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this CoAP message is canceled.
        /// </summary>
        public Boolean Canceled
        {
            get { return _cancelled; }
            set { _cancelled = value; }
        }

        /// <summary>
        /// Gets or sets the type of this CoAP message.
        /// </summary>
        public MessageType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is a request message.
        /// </summary>
        public Boolean IsRequest
        {
            get { return CoAP.Code.IsRequest(_code); }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is a response message.
        /// </summary>
        public Boolean IsResponse
        {
            get { return CoAP.Code.IsResponse(_code); }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is confirmable.
        /// </summary>
        public Boolean IsConfirmable
        {
            get { return _type == MessageType.CON; }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is non-confirmable.
        /// </summary>
        public Boolean IsNonConfirmable
        {
            get { return _type == MessageType.NON; }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is an acknowledgement.
        /// </summary>
        public Boolean IsAcknowledgement
        {
            get { return _type == MessageType.ACK; }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is a reset.
        /// </summary>
        public Boolean IsReset
        {
            get { return _type == MessageType.RST; }
        }

        /// <summary>
        /// Gets a value that indicates whether this CoAP message is an reply message.
        /// </summary>
        public Boolean IsReply
        {
            get { return IsAcknowledgement || IsReset; }
        }

        /// <summary>
        /// Gets a string that is assumed to uniquely identify a message,
        /// since messages from different remote endpoints might have a same message ID.
        /// </summary>
        public String Key
        {
            get
            {
                return String.Format("{0}|{1}#{2}", EndPointID, Type, _id);
            }
        }

        public Int32 Port
        {
            get { return null == this._uri ? CoapConstants.DefaultPort : this._uri.Port; }
        }

        /// <summary>
        /// Gets the IP address of the URI of this CoAP message.
        /// </summary>
        public IPAddress Address
        {
            get
            {
                if (null == this._uri)
                    return null;
                else
                {
                    IPAddress[] addrs = Dns.GetHostAddresses(this._uri.Host);
                    return addrs[0];
                }
            }
        }

        /// <summary>
        /// Gets the endpoint ID of this CoAP message, including ip address and port.
        /// </summary>
        public String EndPointID
        {
            get
            {
                IPAddress addr = Address;

                // TODO 检查IP地址是否需要[]
                return String.Format("[{0}]:{1}", addr, Port);
            }
        }

        /// <summary>
        /// Gets the transfer ID of this CoAP message.
        /// </summary>
        public String TransferID
        {
            get
            {
                Option tokenOpt = GetFirstOption(OptionType.Token);
                String token = (null == tokenOpt) ? String.Empty : tokenOpt.ToString();
                return String.Format("{0}[{1}]", EndPointID, token);
            }
        }

        #endregion

        /// <summary>
        /// Notification method that is called when the transmission of this
        /// message was canceled due to timeout.
        /// <remarks>Subclasses may override this method to add custom handling code.</remarks>
        /// </summary>
        protected virtual void DoHandleTimeout()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        protected virtual void DoHandleBy(IMessageHandler handler)
        { }

        /// <summary>
        /// Creates a message with subtype according to code number
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Message Create(Int32 code)
        {
            if (CoAP.Code.IsRequest(code))
            {
                switch (code)
                {
                    case CoAP.Code.GET:
                        return new GETRequest();
                    case CoAP.Code.POST:
                        return new POSTRequest();
                    case CoAP.Code.PUT:
                        return new PUTRequest();
                    case CoAP.Code.DELETE:
                        return new DELETERequest();
                    default:
                        return new Request();
                }
            }
            else if (CoAP.Code.IsResponse(code))
            {
                return new Response();
            }
            else if (code == CoAP.Code.Empty)
            {
                // empty messages are handled as responses
                // in order to handle ACK/RST messages consistent
                // with actual responses
                return new Response();
            }
            else if (CoAP.Code.IsValid(code))
            {
                return new Message();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes the message from the its binary representation.
        /// </summary>
        /// <param name="bytes">A byte array containing the CoAP encoding of the message</param>
        /// <returns></returns>
        public static Message Decode(Byte[] bytes)
        {
            DatagramReader datagram = new DatagramReader(bytes);

            // read headers
            Int32 version = datagram.Read(VersionBits);
            MessageType type = (MessageType)datagram.Read(TypeBits);
            Int32 optionCount = datagram.Read(OptionCountBits);
            Int32 code = datagram.Read(CodeBits);

            if (!CoAP.Code.IsValid(code))
            {
                if (Log.IsErrorEnabled)
                    Log.Error(null, "Invalid message code: {0}", code);
                return null;
            }

            // create new message with subtype according to code number
            Message msg = Create(code);

            msg._version = version;
            msg._type = type;
            msg._code = code;

            msg._id = datagram.Read(IDBits);

            // read options
            Int32 currentOption = 0;
            for (Int32 i = 0; i < optionCount; i++)
            {
                // read option delta bits
                Int32 optionDelta = datagram.Read(OptionDeltaBits);

                currentOption += optionDelta;
                OptionType currentOptionType = (OptionType)currentOption;

                if (Option.IsFencepost(currentOptionType))
                {
                    // read number of options
                    datagram.Read(OptionLengthBaseBits);
                }
                else
                {
                    // read option length
                    Int32 length = datagram.Read(OptionLengthBaseBits);
                    if (length > MaxOptionLengthBase)
                    {
                        // read extended option length
                        length += datagram.Read(OptionLengthExtendedBits);
                    }
                    // read option
                    Option opt = Option.Create(currentOptionType);
                    opt.RawValue = datagram.ReadBytes(length);

                    msg.AddOption(opt);
                }
            }

            msg.Payload = datagram.ReadBytesLeft();

            // incoming message already have a token, 
            // including implicit empty token
            msg.RequiresToken = false;

            return msg;
        }

        /// <summary>
        /// Matches two messages to buddies if they have the same message ID.
        /// </summary>
        /// <param name="msg1">The first message</param>
        /// <param name="msg2">The second message</param>
        /// <returns>True iif the messages were matched to buddies</returns>
        public static Boolean MatchBuddies(Message msg1, Message msg2)
        {
            if (
                msg1 != null && msg2 != null &&  // both messages must exist
                msg1 != msg2 &&                  // no message can be its own buddy 
                msg1.ID == msg2.ID     // buddy condition: same IDs
            )
            {
                msg1._buddy = msg2;
                msg2._buddy = msg1;

                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void PayloadAppended(Byte[] block)
        { }
    }

    /// <summary>
    /// Types of CoAP messages
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Confirmable messages require an acknowledgement.
        /// </summary>
        CON = 0,
        /// <summary>
        /// Non-Confirmable messages do not require an acknowledgement.
        /// </summary>
        NON,
        /// <summary>
        /// Acknowledgement messages acknowledge a specific confirmable message.
        /// </summary>
        ACK,
        /// <summary>
        /// Reset messages indicate that a specific confirmable message was received, but some context is missing to properly process it.
        /// </summary>
        RST
    }
}
