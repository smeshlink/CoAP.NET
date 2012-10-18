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
using System.IO;

namespace CoAP.Util
{
    /// <summary>
    /// This class describes the functionality to write raw network-ordered datagrams on bit-level.
    /// </summary>
    public class DatagramWriter
    {
        private MemoryStream _stream;
        private Byte _currentByte;
        private Int32 _currentBitIndex;

        /// <summary>
        /// Initializes a new DatagramWriter object
        /// </summary>
        public DatagramWriter()
        {
            _stream = new MemoryStream();
            _currentByte = 0;
            _currentBitIndex = 7;
        }

        /// <summary>
        /// Writes a sequence of bits to the stream
        /// </summary>
        /// <param name="data">An integer containing the bits to write</param>
        /// <param name="numBits">The number of bits to write</param>
        public void Write(Int32 data, Int32 numBits)
        {
            if (numBits < 32 && data >= (1 << numBits))
            {
                Log.Warning(this, "Truncating value {0} to {1}-bit integer", data, numBits);
            }

            for (Int32 i = numBits - 1; i >= 0; i--)
            {
                // test bit
                Boolean bit = (data >> i & 1) != 0;
                if (bit)
                {
                    // set bit in current byte
                    _currentByte |= (Byte)(1 << _currentBitIndex);
                }

                // decrease current bit index
                --_currentBitIndex;

                // check if current byte can be written
                if (_currentBitIndex < 0)
                {
                    WriteCurrentByte();
                }
            }
        }

        /// <summary>
        /// Writes a sequence of bytes to the stream
        /// </summary>
        /// <param name="bytes">The sequence of bytes to write</param>
        public void WriteBytes(byte[] bytes)
        {
            // check if anything to do at all
            if (bytes == null)
                return;

            // are there bits left to write in buffer?
            if (_currentBitIndex < 7)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    Write(bytes[i], 8);
                }
            }
            else
            {
                // if bit buffer is empty, call can be delegated
                // to byte stream to increase
                _stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Returns a byte array containing the sequence of bits written
        /// </summary>
        /// <returns>The byte array containing the written bits</returns>
        public Byte[] ToByteArray()
        {
            WriteCurrentByte();
            Byte[] byteArray = _stream.ToArray();
            _stream.Position = 0;
            return byteArray;
        }

        private void WriteCurrentByte()
        {
            if (_currentBitIndex < 7)
            {
                _stream.WriteByte(_currentByte);
                _currentByte = 0;
                _currentBitIndex = 7;
            }
        }
    }
}
