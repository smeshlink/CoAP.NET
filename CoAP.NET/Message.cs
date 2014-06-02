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
using System.Collections.Generic;
using System.Net;
using System.Text;
using CoAP.Layers;
using CoAP.Log;
using CoAP.Util;

namespace CoAP
{
    /// <summary>
    /// The class Message models the base class of all CoAP messages.
    /// CoAP messages are of type <see cref="Request"/>, <see cref="Response"/>
    /// or <see cref="EmptyMessage"/>, each of which has a <see cref="MessageType"/>,
    /// a message identifier <see cref="Message.ID"/>, a token (0-8 bytes),
    /// a  collection of <see cref="Option"/>s and a payload.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Invalid message ID.
        /// </summary>
        [Obsolete("Use Message.None instead.")]
        public const Int32 InvalidID = None;
        /// <summary>
        /// Indicates that no ID has been set.
        /// </summary>
        public const Int32 None = -1;

        private static readonly ILogger log = LogManager.GetLogger(typeof(Message));

        private Int32 _version = Spec.SupportedVersion;
        private MessageType _type = MessageType.Unknown;
        private Int32 _code;
        private Int32 _id = None;
        private Byte[] _token;
        private Byte[] _payLoadBytes;
        private Boolean _requiresToken = true;
        private Boolean _requiresBlockwise = false;
        private SortedDictionary<OptionType, IList<Option>> _optionMap = new SortedDictionary<OptionType, IList<Option>>();
        private DateTime _timestamp;
        private Int32 _retransmissioned;
        private Int32 _maxRetransmit = CoapConstants.MaxRetransmit;
        private Int32 _responseTimeout = CoapConstants.ResponseTimeout;
        private EndpointAddress _peerAddress;
        private System.Net.EndPoint _source;
        private System.Net.EndPoint _destination;
        private Boolean _acknowledged;
        private Boolean _rejected;
        private Boolean _cancelled;
        private Boolean _timedOut;
        private Boolean _duplicate;
        private Boolean _complete;
        private Byte[] _bytes;
        private ICommunicator _communicator;

        /// <summary>
        /// Occurs when this message is retransmitting.
        /// </summary>
        public event EventHandler Retransmitting;

        /// <summary>
        /// Occurs when this message has been acknowledged by the remote endpoint.
        /// </summary>
        public event EventHandler Acknowledge;

        /// <summary>
        /// Occurs when this message has been rejected by the remote endpoint.
        /// </summary>
        public event EventHandler Reject;

        /// <summary>
        /// Occurs when the client stops retransmitting the message and still has
        /// not received anything from the remote endpoint.
        /// </summary>
        public event EventHandler Timeout;

        /// <summary>
        /// Occurs when this message has been canceled.
        /// </summary>
        public event EventHandler Cancel;

        /// <summary>
        /// Instantiates a message.
        /// </summary>
        public Message()
        { }

        /// <summary>
        /// Instantiates a message with the given type.
        /// </summary>
        /// <param name="type">the message type</param>
        public Message(MessageType type)
        {
            _type = type;
        }

        /// <summary>
        /// Instantiates a message with the given type and code.
        /// </summary>
        /// <param name="type">the message type</param>
        /// <param name="code">the message code</param>
        public Message(MessageType type, Int32 code)
        {
            _type = type;
            _code = code;
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
        /// Gets or sets the ID of this CoAP message.
        /// </summary>
        public Int32 ID
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the 0-8 byte token.
        /// </summary>
        public Byte[] Token
        {
            get { return _token; }
            set
            {
                if (value != null && value.Length > 8)
                    throw new ArgumentException("Token length must be between 0 and 8 inclusive.", "value");

                _token = value;

                if (value != null && value.Length > 0)
                {
                    // for compatibility with CoAP 13-
                    // do not call SetOption() to avoid loop
                    List<Option> list = new List<Option>(1);
                    list.Add(Option.Create(OptionType.Token, value));
                    _optionMap[OptionType.Token] = list;
                }
            }
        }

        /// <summary>
        /// Gets the token represented as a string.
        /// </summary>
        public String TokenString
        {
            get { return _token == null ? null : ByteArrayUtils.ToHexString(_token); }
        }

        /// <summary>
        /// Gets the size of the payload of this CoAP message.
        /// </summary>
        public Int32 PayloadSize
        {
            get { return (null == _payLoadBytes) ? 0 : _payLoadBytes.Length; }
        }

        /// <summary>
        /// Gets or sets the payload of this CoAP message.
        /// </summary>
        public Byte[] Payload
        {
            get { return _payLoadBytes; }
            set { _payLoadBytes = value; }
        }

        /// <summary>
        /// Gets or sets the payload of this CoAP message in string representation.
        /// </summary>
        public String PayloadString
        {
            get { return (null == _payLoadBytes) ? null : System.Text.Encoding.UTF8.GetString(_payLoadBytes); }
            set { SetPayload(value, MediaType.TextPlain); }
        }

        /// <summary>
        /// Gets or sets the destination endpoint.
        /// </summary>
        public System.Net.EndPoint Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        /// <summary>
        /// Gets or sets the source endpoint.
        /// </summary>
        public System.Net.EndPoint Source
        {
            get { return _source; }
            set { _source = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this message has been acknowledged.
        /// </summary>
        public Boolean Acknowledged
        {
            get { return _acknowledged; }
            set
            {
                _acknowledged = value;
                if (value)
                    Fire(Acknowledge);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this message has been rejected.
        /// </summary>
        public Boolean Rejected
        {
            get { return _rejected; }
            set
            {
                _rejected = value;
                if (value)
                    Fire(Reject);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this CoAP message has timed out.
        /// Confirmable messages in particular might timeout.
        /// </summary>
        public Boolean TimedOut
        {
            get { return _timedOut; }
            set
            {
                _timedOut = value;
                if (value)
                    Fire(Timeout);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this CoAP message is canceled.
        /// </summary>
        public Boolean Canceled
        {
            get { return _cancelled; }
            set
            {
                _cancelled = value;
                if (value)
                    Fire(Cancel);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this message is a duplicate.
        /// </summary>
        public Boolean Duplicate
        {
            get { return _duplicate; }
            set { _duplicate = value; }
        }

        /// <summary>
        /// Gets or sets the serialized message as byte array, or null if not serialized yet.
        /// </summary>
        public Byte[] Bytes
        {
            get { return _bytes; }
            set { _bytes = value; }
        }

        /// <summary>
        /// Gets or sets the timestamp when this message has been received or sent,
        /// or <see cref="DateTime.MinValue"/> if neither has happened yet.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        internal void FireRetransmitting()
        {
            Fire(Retransmitting);
        }

        private void Fire(EventHandler handler)
        {
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Creates a reply message to this message, which addressed to the
        /// peer and has the same message ID and token.
        /// </summary>
        /// <param name="ack">Acknowledgement or not</param>
        /// <returns></returns>
        public Message NewReply(Boolean ack)
        {
            return NewReply(CoAP.Code.Empty, ack);
        }

        public Message NewReply(Int32 code, Boolean ack)
        {
            Message reply = new Message(_type == MessageType.CON ?
                (ack ? MessageType.ACK : MessageType.RST) : MessageType.NON, code);

            // echo ID
            reply._id = this._id;
            // set the receiver URI of the reply to the sender of this message
            reply._peerAddress = _peerAddress;

            // echo token
            reply.Token = Token;
            reply.RequiresToken = this.RequiresToken;

            return reply;
        }

        /// <summary>
        /// Creates a new ACK message with peer address and MID matching to this message.
        /// </summary>
        public Message NewAccept()
        {
            Message ack = new Message(MessageType.ACK, CoAP.Code.Empty);
            ack.PeerAddress = this.PeerAddress;
            ack.ID = this.ID;
            // echo token
            ack.Token = Token;
            return ack;
        }

        /// <summary>
        /// Creates a new RST message with peer address and MID matching to this message.
        /// </summary>
        public Message NewReject()
        {
            Message rst = new Message(MessageType.RST, CoAP.Code.Empty);
            rst.PeerAddress = this.PeerAddress;
            rst.ID = this.ID;
            return rst;
        }

        /// <summary>
        /// Accepts this message with an empty ACK. Use this method only at
        /// application level, as the ACK will be sent through the whole stack.
        /// Within the stack use NewAccept() and send it through the corresponding lower layer.
        /// </summary>
        public virtual void Accept()
        {
            if (IsConfirmable)
            {
                Message msg = NewAccept();
                msg.Communicator = this.Communicator;
                msg.Send();
            }
        }

        /// <summary>
        /// Rejects this message with an empty RST. Use this method only at
        /// application level, as the RST will be sent through the whole stack.
        /// Within the stack use NewReject() and send it through the corresponding lower layer.
        /// </summary>
        public void Reject0()
        {
            Message msg = NewReject();
            msg.Communicator = this.Communicator;
            msg.Send();
        }

        /// <summary>
        /// Sends this message.
        /// </summary>
        public void Send()
        {
            Communicator.SendMessage(this);
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
            if (payload == null)
                payload = String.Empty;
            Payload = System.Text.Encoding.UTF8.GetBytes(payload);
        }

        /// <summary>
        /// Sets the payload of this CoAP message.
        /// </summary>
        /// <param name="payload">The string representation of the payload</param>
        /// <param name="mediaType">The content-type of the payload</param>
        public void SetPayload(String payload, Int32 mediaType)
        {
            if (payload == null)
                payload = String.Empty;
            Payload = System.Text.Encoding.UTF8.GetBytes(payload);
            ContentType = mediaType;
        }

        /// <summary>
        /// To string.
        /// </summary>
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

        /// <summary>
        /// Equals.
        /// </summary>
        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            if (this.GetType() != obj.GetType())
                return false;
            Message other = (Message)obj;
            if (_type != other._type)
                return false;
            if (_version != other._version)
                return false;
            if (_code != other._code)
                return false;
            if (_id != other._id)
                return false;
            if (_optionMap == null)
            {
                if (other._optionMap != null)
                    return false;
            }
            else if (!_optionMap.Equals(other._optionMap))
                return false;
            if (!Sort.IsSequenceEqualTo(_payLoadBytes, other._payLoadBytes))
                return false;
            if (PeerAddress == null)
            {
                if (other.PeerAddress != null)
                    return false;
            }
            else if (!PeerAddress.Equals(other.PeerAddress))
                return false;
            return true;
        }

        /// <summary>
        /// Get hash code.
        /// </summary>
        public override Int32 GetHashCode()
        {
            return base.GetHashCode();
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
                Token = option.RawValue;
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

        public ICommunicator Communicator
        {
            get
            {
                if (_communicator == null)
                    _communicator = CommunicatorFactory.Default;
                return _communicator;
            }
            set { _communicator = value; }
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
        /// Gets or sets how many times this message has been retransmissioned.
        /// </summary>
        public Int32 Retransmissioned
        {
            get { return _retransmissioned; }
            set { _retransmissioned = value; }
        }

        /// <summary>
        /// Gets or sets the max times this message should be retransmissioned.
        /// By default the value is equal to <code>CoapConstants.MaxRetransmit</code>.
        /// A value of 0 indicates that this message will not be retransmissioned when timeout.
        /// </summary>
        public Int32 MaxRetransmit
        {
            get { return _maxRetransmit; }
            set { _maxRetransmit = value; }
        }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds after which this message will time out.
        /// The default value is <code>CoapConstants.ResponseTimeout</code>.
        /// A value less or equal than 0 indicates an infinite time-out period.
        /// </summary>
        public Int32 ResponseTimeout
        {
            get { return _responseTimeout; }
            set { _responseTimeout = value; }
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

        public Int32 FirstAccept
        {
            get {
                Option opt = GetFirstOption(OptionType.Accept);
                return opt == null ? MediaType.Undefined : opt.IntValue;
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

        public String LocationQuery
        {
            get { return Option.Join(GetOptions(OptionType.LocationQuery), "&"); }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.StartsWith("?"))
                    value = value.Substring(1);
                SetOptions(Option.Split(OptionType.LocationQuery, value, "&"));
            }
        }

        public Uri ProxyUri
        {
            get
            {
                IEnumerable<Option> opts = GetOptions(OptionType.ProxyUri);
                if (opts == null)
                    return null;

                String proxyUriString = Uri.UnescapeDataString(Option.Join(opts, "/"));
                // TODO URLDecode
                if (!proxyUriString.StartsWith("coap://") && !proxyUriString.StartsWith("coaps://")
                    && !proxyUriString.StartsWith("http://") && !proxyUriString.StartsWith("https://"))
                    proxyUriString = "coap://" + proxyUriString;
                return new Uri(proxyUriString);
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
        /// Gets the code of this CoAP message.
        /// </summary>
        public Int32 Code
        {
            get { return _code; }
        }

        /// <summary>
        /// Gets the code's string representation of this CoAP message.
        /// </summary>
        public String CodeString
        {
            get { return CoAP.Code.ToString(_code); }
        }

        /// <summary>
        /// Gets the version of this CoAP message.
        /// </summary>
        public Int32 Version
        {
            get { return _version; }
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

        public EndpointAddress PeerAddress
        {
            get { return _peerAddress; }
            set { _peerAddress = value; }
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

        protected virtual void PayloadAppended(Byte[] block)
        { }
    }
}
