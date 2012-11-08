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
using System.IO;

namespace CoAP.Util
{
    /// <summary>
    /// This class describes the functionality to read raw network-ordered datagrams on bit-level.
    /// </summary>
    public class DatagramReader
    {
        private MemoryStream _stream;
        private Byte _currentByte;
        private Int32 _currentBitIndex;

        /// <summary>
        /// Initializes a new DatagramReader object
        /// </summary>
        /// <param name="buffer">The byte array to read from</param>
        public DatagramReader(Byte[] buffer)
        {
            _stream = new MemoryStream(buffer, false);
            _currentByte = 0;
            _currentBitIndex = -1;
        }

        /// <summary>
        /// Reads a sequence of bits from the stream
        /// </summary>
        /// <param name="numBits">The number of bits to read</param>
        /// <returns>An integer containing the bits read</returns>
        public Int32 Read(Int32 numBits)
        {
            Int32 bits = 0; // initialize all bits to zero
            for (Int32 i = numBits - 1; i >= 0; i--)
            {
                // check whether new byte needs to be read
                if (_currentBitIndex < 0)
                {
                    ReadCurrentByte();
                }

                // test current bit
                Boolean bit = (_currentByte >> _currentBitIndex & 1) != 0;
                if (bit)
                {
                    // set bit at i-th position
                    bits |= (1 << i);
                }

                // decrease current bit index
                --_currentBitIndex;
            }
            return bits;
        }

        /// <summary>
        /// Reads a sequence of bytes from the stream
        /// </summary>
        /// <param name="count">The number of bytes to read</param>
        /// <returns>The sequence of bytes read from the stream</returns>
        public Byte[] ReadBytes(Int32 count)
        {
            // for negative count values, read all bytes left
            if (count < 0)
                count = (Int32)(_stream.Length - _stream.Position);

            Byte[] bytes = new Byte[count];

            // are there bits left to read in buffer?
            if (_currentBitIndex >= 0)
            {
                for (Int32 i = 0; i < count; i++)
                {
                    bytes[i] = (Byte)Read(8);
                }
            }
            else
            {
                _stream.Read(bytes, 0, bytes.Length);
            }

            return bytes;
        }

        /// <summary>
        /// Reads the complete sequence of bytes left in the stream
        /// </summary>
        /// <returns>The sequence of bytes left in the stream</returns>
        public Byte[] ReadBytesLeft()
        {
            return ReadBytes(-1);
        }

        private void ReadCurrentByte()
        {
            Int32 val = _stream.ReadByte();
            // TODO 判断EOF
            _currentByte = (Byte)val;
            // reset current bit index
            _currentBitIndex = 7;
        }
    }
}
