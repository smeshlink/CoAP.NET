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
using System.Net;
using System.Text;
using CoAP.Log;
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// This class describes the functionality of the CoAP messages
    /// </summary>
    public class Message
    {
        const Int32 SupportedVersion = 1;
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

        private static ILogger log = LogManager.GetLogger(typeof(Message));
       
        private Int32 _version = SupportedVersion;
        private MessageType _type;
        private Int32 _code;
        private Int32 _id = InvalidID;
        private Byte[] _payLoadBytes;
        private Message _buddy;
        protected Boolean _requiresToken = true;
        protected Boolean _requiresBlockwise = false;
        private SortedDictionary<OptionType, IList<Option>> _optionMap = new SortedDictionary<OptionType, IList<Option>>();
        private Int64 _timestamp;
        private Uri _uri;
        private Boolean _cancelled = false;
        private Boolean _complete = false;
        private Communicator _communicator = Communicator.Instance;

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

        public Message NewAccept()
        {
            Message ack = new Message(MessageType.ACK, CoAP.Code.Empty);
            ack.PeerAddress = this.PeerAddress;
            ack.ID = this.ID;
            // echo token
            ack.SetOption(GetFirstOption(OptionType.Token));
            return ack;
        }

        public Message NewReject()
        {
            Message rst = new Message(MessageType.RST, CoAP.Code.Empty);
            rst.PeerAddress = this.PeerAddress;
            rst.ID = this.ID;
            return rst;
        }

        public virtual void Accept()
        {
            if (IsConfirmable)
            {
                NewAccept().Send();
            }
        }

        public void Reject()
        {
            NewReject().Send();
        }

        public void Send()
        {
            _communicator.SendMessage(this);
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
                PayloadAppended(block);
                System.Threading.Monitor.Exit(this);
            }
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        public void SetPayload(String payload)
        {
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
            foreach (Option opt in GetOptions())
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
                        if (log.IsWarnEnabled)
                            log.Warn("Fencepost liveness violated: delta = " + fencepostDelta);
                    }
                    if (fencepostDelta > MaxOptionDelta)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("Fencepost safety violated: delta = " + fencepostDelta);
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
#if DEBUG
            StringBuilder builder = new StringBuilder();
            String kind = "MESSAGE";
            if (this.IsRequest)
                kind = "REQUEST";
            else if (this.IsResponse)
                kind = "RESPONSE";
            builder.AppendFormat("==[ COAP {0} ]============================================\n", kind);

            IList<Option> options = GetOptions();
            builder.AppendFormat("Address:  {0}\n", PeerAddress == null ? "local" : PeerAddress.ToString());
            builder.AppendFormat("ID     :  {0}\n", _id);
            builder.AppendFormat("Type   :  {0}\n", Type);
            builder.AppendFormat("Code   :  {0}\n", CoAP.Code.ToString(_code));
            builder.AppendFormat("Options:  {0}\n", options.Count);
            foreach (Option opt in options)
            {
                builder.AppendFormat("  * {0}: {1} ({2} Bytes)\n", opt.Name, opt, opt.Length);
            }
            builder.AppendFormat("Payload: {0} Bytes\n", this.PayloadSize);
            if (this.PayloadSize > 0)
            {
                builder.AppendLine("---------------------------------------------------------------");
                builder.AppendLine(this.PayloadString);
            }
            builder.AppendLine("===============================================================");

            return builder.ToString();
#else
            return String.Format("{0}: [{1}] {2} '{3}'({4})",
                Key, Type, CoAP.Code.ToString(_code),
                PayloadString, PayloadSize);
#endif
        }

        #region Option operations

        /// <summary>
        /// Adds an option to the list of options of this CoAP message.
        /// </summary>
        /// <param name="option">the option to add</param>
        public void AddOption(Option option)
        {
            if (option == null)
                throw new ArgumentNullException("opt");

            IList<Option> list = null;
            if (_optionMap.ContainsKey(option.Type))
                list = _optionMap[option.Type];
            else
            {
                list = new List<Option>();
                _optionMap[option.Type] = list;
            }

            list.Add(option);

            if (option.Type == OptionType.Token)
                _requiresToken = false;
        }

        /// <summary>
        /// Adds all option to the list of options of this CoAP message.
        /// </summary>
        /// <param name="options">the options to add</param>
        public void AddOptions(IEnumerable<Option> options)
        {
            foreach (Option opt in options)
            {
                AddOption(opt);
            }
        }

        /// <summary>
        /// Removes all options of the given type from this CoAP message.
        /// </summary>
        /// <param name="optionType">the type of option to remove</param>
        public void RemoveOptions(OptionType optionType)
        {
            _optionMap.Remove(optionType);
        }

        /// <summary>
        /// Gets all options of the given type.
        /// </summary>
        /// <param name="optionType">the option type</param>
        /// <returns></returns>
        public IEnumerable<Option> GetOptions(OptionType optionType)
        {
            return _optionMap.ContainsKey(optionType) ? _optionMap[optionType] : null;
        }

        /// <summary>
        /// Sets an option.
        /// </summary>
        /// <param name="opt">the option to set</param>
        public void SetOption(Option opt)
        {
            if (null != opt)
            {
                RemoveOptions(opt.Type);
                AddOption(opt);
            }
        }

        /// <summary>
        /// Sets all options with the specified option type.
        /// </summary>
        /// <param name="options">the options to set</param>
        public void SetOptions(IEnumerable<Option> options)
        {
            if (options == null)
                return;
            foreach (Option opt in options)
            {
                RemoveOptions(opt.Type);
            }
            AddOptions(options);
        }

        /// <summary>
        /// Checks if this CoAP message has options of the specified option type.
        /// </summary>
        /// <param name="type">the option type</param>
        /// <returns>rrue if options of the specified type exist</returns>
        public Boolean HasOption(OptionType type)
        {
            return GetFirstOption(type) != null;
        }

        /// <summary>
        /// Gets the first option of the specified option type.
        /// </summary>
        /// <param name="optionType">the option type</param>
        /// <returns>the first option of the specified type, or null</returns>
        public Option GetFirstOption(OptionType optionType)
        {
            IList<Option> list = _optionMap.ContainsKey(optionType) ? _optionMap[optionType] : null;
            return (null != list && list.Count > 0) ? list[0] : null;
        }

        /// <summary>
        /// Gets a sorted list of all options.
        /// </summary>
        /// <returns></returns>
        public IList<Option> GetOptions()
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
            return GetOptions().Count;
        }

        #endregion

        #region Properties

        public Communicator Communicator
        {
            get { return _communicator; }
            set { _communicator = value; }
        }

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
            get { return _requiresToken && Code != CoAP.Code.Empty; }
            set { _requiresToken = value; }
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
                    // TODO Uri-Host option

                    UriPath = value.AbsolutePath;
                    Query = value.Query;
                    PeerAddress = new EndpointAddress(value);
                }
                this._uri = value;
            }
        }

        public String UriPath
        {
            get { return Option.Join(GetOptions(OptionType.UriPath), "/"); }
            set { SetOptions(Option.Split(OptionType.UriPath, value, "/")); }
        }

        public String Query
        {
            get { return Option.Join(GetOptions(OptionType.UriQuery), "&"); }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.StartsWith("?"))
                    value = value.Substring(1);
                SetOptions(Option.Split(OptionType.UriQuery, value, "&"));
            }
        }

        public Byte[] Token
        {
            get
            {
                Option opt = GetFirstOption(OptionType.Token);
                return opt == null ? TokenManager.EmptyToken : opt.RawValue;
            }
            set
            {
                SetOption(Option.Create(OptionType.Token, value));
            }
        }

        public String TokenString
        {
            get
            {
                Byte[] token = Token;
                if (token == null || token.Length == 0)
                    return "--";
                else
                    return BitConverter.ToString(token);
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
                    RemoveOptions(OptionType.ContentType);
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
                SetOptions(Option.Split(OptionType.LocationPath, value, "/"));
            }
        }

        /// <summary>
        /// Gets or sets the max-age of this CoAP message.
        /// </summary>
        public Int32 MaxAge
        {
            get
            {
                Option opt = GetFirstOption(OptionType.MaxAge);
                return (null == opt) ? CoapConstants.DefaultMaxAge : opt.IntValue;
            }
            set
            {
                SetOption(Option.Create(OptionType.MaxAge, value));
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
        /// Gets a value that indicates whether this response is a separate one.
        /// </summary>
        public Boolean IsEmptyACK
        {
            get { return IsAcknowledgement && Code == CoAP.Code.Empty; }
        }

        /// <summary>
        /// Gets a string that is assumed to uniquely identify a message,
        /// since messages from different remote endpoints might have a same message ID.
        /// </summary>
        public String Key
        {
            get
            {
                return String.Format("{0}|{1}|{2}", PeerAddress == null ? "local" : PeerAddress.ToString(), _id, Type);
            }
        }

        public String TransactionKey
        {
            get
            {
                return String.Format("{0}|{1}", PeerAddress == null ? "local" : PeerAddress.ToString(), _id);
            }
        }

        public String SequenceKey
        {
            get
            {
                return String.Format("{0}#{1}", PeerAddress == null ? "local" : PeerAddress.ToString(), TokenString);
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

        public EndpointAddress PeerAddress { get; set; }

        public Int32 Retransmissioned { get; set; }

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
                        return new UnsupportedRequest(code);
                }
            }
            else if (CoAP.Code.IsResponse(code))
            {
                return new Response(code);
            }
            else if (code == CoAP.Code.Empty)
            {
                // empty messages are handled as responses
                // in order to handle ACK/RST messages consistent
                // with actual responses
                return new Response(code);
            }
            else
            {
                return new Message(MessageType.CON, code);
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
            if (version != SupportedVersion)
                return null;

            MessageType type = (MessageType)datagram.Read(TypeBits);
            Int32 optionCount = datagram.Read(OptionCountBits);
            Int32 code = datagram.Read(CodeBits);

            // create new message with subtype according to code number
            Message msg = Create(code);

            msg._type = type;
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
}
