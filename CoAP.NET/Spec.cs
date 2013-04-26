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

namespace CoAP
{
#if COAPALL
    public static class Spec
    {
        public const Int32 SupportedVersion = 1;
        public static readonly ISpec Draft08 = new CoAP.Draft08();
        public static readonly ISpec Draft12 = new CoAP.Draft12();
    }

    public interface ISpec
    {
        Int32 SupportedVersion { get; }
        Int32 DefaultPort { get; }
        Byte[] Encode(Message msg);
        Message Decode(Byte[] bytes);
        OptionType GetOptionType(Int32 optionNumber);
    }
#endif

#if COAPALL || COAP08
#if COAP08
    public static class Spec
#else
    class Draft08 : ISpec
#endif
    {
        const Int32 VersionBits = 2;
        const Int32 TypeBits = 2;
        const Int32 OptionCountBits = 4;
        const Int32 CodeBits = 8;
        const Int32 IDBits = 16;
        const Int32 OptionDeltaBits = 4;
        const Int32 OptionLengthBaseBits = 4;
        const Int32 OptionLengthExtendedBits = 8;
        const Int32 MaxOptionDelta = (1 << OptionDeltaBits) - 1;
        const Int32 MaxOptionLengthBase = (1 << OptionLengthBaseBits) - 2;

        static readonly ILogger log = LogManager.GetLogger(typeof(Spec));

#if COAP08
        public const Int32 SupportedVersion = 1;
        public const Int32 DefaultPort = 5683;
#else
        public Int32 SupportedVersion { get { return 1; } }
        public Int32 DefaultPort { get { return 5683; } }
#endif

#if COAP08
        public static Byte[] Encode(Message msg)
#else
        public Byte[] Encode(Message msg)
#endif
        {
            // create datagram writer to encode options
            DatagramWriter optWriter = new DatagramWriter();
            Int32 optionCount = 0;
            Int32 lastOptionNumber = 0;

            List<Option> options = (List<Option>)msg.GetOptions();
            Sort.InsertionSort(options, delegate(Option o1, Option o2)
            {
                return GetOptionNumber(o1.Type).CompareTo(GetOptionNumber(o2.Type));
            });

            foreach (Option opt in options)
            {
                if (opt.IsDefault)
                    continue;

                Int32 optNum = GetOptionNumber(opt.Type);
                Int32 optionDelta = optNum - lastOptionNumber;

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
                lastOptionNumber = optNum;
            }

            // create datagram writer to encode message data
            DatagramWriter writer = new DatagramWriter();

            // write fixed-size CoAP headers
            writer.Write(msg.Version, VersionBits);
            writer.Write((Int32)msg.Type, TypeBits);
            writer.Write(optionCount, OptionCountBits);
            writer.Write(msg.Code, CodeBits);
            writer.Write(msg.ID, IDBits);

            // write options
            writer.WriteBytes(optWriter.ToByteArray());

            //write payload
            writer.WriteBytes(msg.Payload);

            return writer.ToByteArray();
        }

#if COAP08
        public static Message Decode(Byte[] bytes)
#else
        public Message Decode(Byte[] bytes)
#endif
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
            Message msg = Message.Create(code);

            msg.Type = type;
            msg.ID = datagram.Read(IDBits);

            // read options
            Int32 currentOption = 0;
            for (Int32 i = 0; i < optionCount; i++)
            {
                // read option delta bits
                Int32 optionDelta = datagram.Read(OptionDeltaBits);

                currentOption += optionDelta;
                OptionType currentOptionType = GetOptionType(currentOption);

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

#if COAP08
        public static Int32 GetOptionNumber(OptionType optionType)
#else
        public Int32 GetOptionNumber(OptionType optionType)
#endif
        {
            switch (optionType)
            {
                case OptionType.Reserved:
                    return 0;
                case OptionType.ContentType:
                    return 1;
                case OptionType.MaxAge:
                    return 2;
                case OptionType.ProxyUri:
                    return 3;
                case OptionType.ETag:
                    return 4;
                case OptionType.UriHost:
                    return 5;
                case OptionType.LocationPath:
                    return 6;
                case OptionType.UriPort:
                    return 7;
                case OptionType.LocationQuery:
                    return 8;
                case OptionType.UriPath:
                    return 9;
                case OptionType.Token:
                    return 11;
                case OptionType.UriQuery:
                    return 15;
                case OptionType.Observe:
                    return 10;
                case OptionType.Accept:
                    return 12;
                case OptionType.IfMatch:
                    return 13;
                case OptionType.Block2:
                    return 17;
                case OptionType.Block1:
                    return 19;
                case OptionType.IfNoneMatch:
                    return 21;
                default:
                    return (Int32)optionType;
            }
        }

#if COAP08
        public static OptionType GetOptionType(Int32 optionNumber)
#else
        public OptionType GetOptionType(Int32 optionNumber)
#endif
        {
            switch (optionNumber)
            {
                case 0:
                    return OptionType.Reserved;
                case 1:
                    return OptionType.ContentType;
                case 2:
                    return OptionType.MaxAge;
                case 3:
                    return OptionType.ProxyUri;
                case 4:
                    return OptionType.ETag;
                case 5:
                    return OptionType.UriHost;
                case 6:
                    return OptionType.LocationPath;
                case 7:
                    return OptionType.UriPort;
                case 8:
                    return OptionType.LocationQuery;
                case 9:
                    return OptionType.UriPath;
                case 11:
                    return OptionType.Token;
                case 15:
                    return OptionType.UriQuery;
                case 10:
                    return OptionType.Observe;
                case 12:
                    return OptionType.Accept;
                case 13:
                    return OptionType.IfMatch;
                case 17:
                    return OptionType.Block2;
                case 19:
                    return OptionType.Block1;
                case 21:
                    return OptionType.IfNoneMatch;
                default:
                    return (OptionType)optionNumber;
            }
        }
    }
#endif

#if COAPALL || COAP12
#if COAP12
    public static class Spec
#else
    class Draft12 : ISpec
#endif
    {
        const Int32 VersionBits = 2;
        const Int32 TypeBits = 2;
        const Int32 OptionCountBits = 4;
        const Int32 CodeBits = 8;
        const Int32 IDBits = 16;
        const Int32 OptionDeltaBits = 4;
        const Int32 OptionLengthBaseBits = 4;
        const Int32 OptionLengthExtendedBits = 8;
        const Int32 MaxOptionDelta = 14;
        const Int32 SingleOptionJumpBits = 8;
        const Int32 MaxOptionLengthBase = (1 << OptionLengthBaseBits) - 2;

#if COAP12
        public const Int32 SupportedVersion = 1;
        public const Int32 DefaultPort = 5683;
#else
        public Int32 SupportedVersion { get { return 1; } }
        public Int32 DefaultPort { get { return 5683; } }
#endif

#if COAP12
        public static Byte[] Encode(Message msg)
#else
        public Byte[] Encode(Message msg)
#endif
        {
            // create datagram writer to encode options
            DatagramWriter optWriter = new DatagramWriter();
            Int32 optionCount = 0;
            Int32 lastOptionNumber = 0;

            List<Option> options = (List<Option>)msg.GetOptions();
            Sort.InsertionSort(options, delegate(Option o1, Option o2)
            {
                return GetOptionNumber(o1.Type).CompareTo(GetOptionNumber(o2.Type));
            });

            foreach (Option opt in options)
            {
                if (opt.IsDefault)
                    continue;

                Int32 optNum = GetOptionNumber(opt.Type);
                Int32 optionDelta = optNum - lastOptionNumber;

                /*
                 * The Option Jump mechanism is used when the delta to the next option
                 * number is larger than 14.
                 */
                if (optionDelta > MaxOptionDelta)
                {
                    /*
                     * For the formats that include an Option Jump Value, the actual
                     * addition to the current Option number is computed as follows:
                     * Delta = ((Option Jump Value) + N) * 8 where N is 2 for the
                     * one-byte version and N is 258 for the two-byte version.
                     */
                    if (optionDelta < 30)
                    {
                        optWriter.Write(0xF1, SingleOptionJumpBits);
                        optionDelta -= 15;
                    }
                    else if (optionDelta < 2064)
                    {
                        Int32 optionJumpValue = (optionDelta / 8) - 2;
                        optionDelta -= (optionJumpValue + 2) * 8;
                        optWriter.Write(0xF2, SingleOptionJumpBits);
                        optWriter.Write(optionJumpValue, SingleOptionJumpBits);
                    }
                    else if (optionDelta < 526359)
                    {
                        optionDelta = Math.Min(optionDelta, 526344); // Limit to avoid overflow
                        Int32 optionJumpValue = (optionDelta / 8) - 258;
                        optionDelta -= (optionJumpValue + 258) * 8;
                        optWriter.Write(0xF3, SingleOptionJumpBits);
                        optWriter.Write(optionJumpValue, 2 * SingleOptionJumpBits);
                    }
                    else
                    {
                        throw new Exception("Option delta too large. Actual delta: " + optionDelta);
                    }
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
                else if (length <= 1034)
                {
                    /*
                     * When the Length field is set to 15, another byte is added as
                     * an 8-bit unsigned integer whose value is added to the 15,
                     * allowing option value lengths of 15-270 bytes. For option
                     * lengths beyond 270 bytes, we reserve the value 255 of an
                     * extension byte to mean
                     * "add 255, read another extension byte". Options that are
                     * longer than 1034 bytes MUST NOT be sent
                     */
                    optWriter.Write(15, OptionLengthBaseBits);

                    Int32 rounds = (length - 15) / 255;
                    for (Int32 i = 0; i < rounds; i++)
                    {
                        optWriter.Write(255, OptionLengthExtendedBits);
                    }
                    Int32 remainingLength = length - ((rounds * 255) + 15);
                    optWriter.Write(remainingLength, OptionLengthExtendedBits);
                }
                else
                {
                    throw new Exception("Option length larger than allowed 1034. Actual length: " + length);
                }

                // write option value
                if (length > 0)
                    optWriter.WriteBytes(opt.RawValue);

                ++optionCount;
                lastOptionNumber = optNum;
            }

            // create datagram writer to encode message data
            DatagramWriter writer = new DatagramWriter();

            // write fixed-size CoAP headers
            writer.Write(msg.Version, VersionBits);
            writer.Write((Int32)msg.Type, TypeBits);
            if (optionCount < 15)
                writer.Write(optionCount, OptionCountBits);
            else
                writer.Write(15, OptionCountBits);
            writer.Write(msg.Code, CodeBits);
            writer.Write(msg.ID, IDBits);

            // write options
            writer.WriteBytes(optWriter.ToByteArray());

            if (optionCount > 14)
            {
                // end-of-options marker when there are more than 14 options
                writer.Write(0xf0, 8);
            }

            //write payload
            writer.WriteBytes(msg.Payload);

            return writer.ToByteArray();
        }

#if COAP12
        public static Message Decode(Byte[] bytes)
#else
        public Message Decode(Byte[] bytes)
#endif
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
            Message msg = Message.Create(code);

            msg.Type = type;
            msg.ID = datagram.Read(IDBits);

            // read options
            Int32 currentOption = 0;
            Boolean hasMoreOptions = optionCount == 15;
            for (Int32 i = 0; (i < optionCount || hasMoreOptions) && datagram.BytesAvailable; i++)
            {
                // first 4 option bits: either option jump or option delta
                Int32 optionDelta = datagram.Read(OptionDeltaBits);

                if (optionDelta == 15)
                {
                    // option jump or end-of-options marker
                    Int32 bits = datagram.Read(4);
                    switch (bits)
                    {
                        case 0:
                            // end-of-options marker read (0xF0), payload follows
                            hasMoreOptions = false;
                            continue;
                        case 1:
                            // 0xF1 (Delta = 15)
                            optionDelta = 15 + datagram.Read(OptionDeltaBits);
                            break;
                        case 2:
                            // Delta = ((Option Jump Value) + 2) * 8
                            optionDelta = (datagram.Read(8) + 2) * 8 + datagram.Read(OptionDeltaBits);
                            break;
                        case 3:
                            // Delta = ((Option Jump Value) + 258) * 8
                            optionDelta = (datagram.Read(16) + 258) * 8 + datagram.Read(OptionDeltaBits);
                            break;
                        default:
                            break;
                    }
                }

                currentOption += optionDelta;
                OptionType currentOptionType = GetOptionType(currentOption);

                Int32 length = datagram.Read(OptionLengthBaseBits);
                if (length == 15)
                {
                    /*
                     * When the Length field is set to 15, another byte is added as
                     * an 8-bit unsigned integer whose value is added to the 15,
                     * allowing option value lengths of 15-270 bytes. For option
                     * lengths beyond 270 bytes, we reserve the value 255 of an
                     * extension byte to mean
                     * "add 255, read another extension byte".
                     */
                    Int32 additionalLength = 0;
                    do
                    {
                        additionalLength = datagram.Read(8);
                        length += additionalLength;
                    } while (additionalLength >= 255);
                }

                // read option
                Option opt = Option.Create(currentOptionType);
                opt.RawValue = datagram.ReadBytes(length);

                msg.AddOption(opt);
            }

            msg.Payload = datagram.ReadBytesLeft();

            // incoming message already have a token, including implicit empty token
            msg.RequiresToken = false;

            return msg;
        }

#if COAP12
        public static Int32 GetOptionNumber(OptionType optionType)
#else
        public Int32 GetOptionNumber(OptionType optionType)
#endif
        {
            switch (optionType)
            {
                // draft-ietf-core-coap-12
                case OptionType.Reserved:
                    return 0;
                case OptionType.IfMatch:
                    return 1;
                case OptionType.UriHost:
                    return 3;
                case OptionType.ETag:
                    return 4;
                case OptionType.IfNoneMatch:
                    return 5;
                case OptionType.UriPort:
                    return 7;
                case OptionType.LocationPath:
                    return 8;
                case OptionType.UriPath:
                    return 11;
                case OptionType.ContentType:
                    return 12;
                case OptionType.MaxAge:
                    return 14;
                case OptionType.UriQuery:
                    return 15;
                case OptionType.Accept:
                    return 16;
                case OptionType.Token:
                    return 19;
                case OptionType.LocationQuery:
                    return 20;
                case OptionType.ProxyUri:
                    return 35;
                // draft-ietf-core-observe-07
                case OptionType.Observe:
                    return 6;
                // draft-ietf-core-block-08
                case OptionType.Block2:
                    return 23;
                case OptionType.Block1:
                    return 27;
                case OptionType.Size:
                    return 28;
                default:
                    return (Int32)optionType;
            }
        }

#if COAP12
        public static OptionType GetOptionType(Int32 optionNumber)
#else
        public OptionType GetOptionType(Int32 optionNumber)
#endif
        {
            switch (optionNumber)
            {
                case 0:
                    return OptionType.Reserved;
                case 1:
                    return OptionType.IfMatch;
                case 3:
                    return OptionType.UriHost;
                case 4:
                    return OptionType.ETag;
                case 5:
                    return OptionType.IfNoneMatch;
                case 7:
                    return OptionType.UriPort;
                case 8:
                    return OptionType.LocationPath;
                case 11:
                    return OptionType.UriPath;
                case 12:
                    return OptionType.ContentType;
                case 14:
                    return OptionType.MaxAge;
                case 15:
                    return OptionType.UriQuery;
                case 16:
                    return OptionType.Accept;
                case 19:
                    return OptionType.Token;
                case 20:
                    return OptionType.LocationQuery;
                case 35:
                    return OptionType.ProxyUri;
                case 6:
                    return OptionType.Observe;
                case 23:
                    return OptionType.Block2;
                case 27:
                    return OptionType.Block1;
                case 28:
                    return OptionType.Size;
                default:
                    return (OptionType)optionNumber;
            }
        }
    }
#endif
}
