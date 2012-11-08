using System;
using CoAP.Util;

namespace CoAP.Test
{
    class DatagramReadWriteTest
    {
        public void Test32BitInt()
        {
            unchecked
            {
                Int32 intIn = (Int32)0x87654321;

                DatagramWriter writer = new DatagramWriter();
                writer.Write(intIn, 32);

                DatagramReader reader = new DatagramReader(writer.ToByteArray());
                Int32 intOut = reader.Read(32);

                Assert.IsEqualTo(intIn, intOut);
            }
        }

        public void Test32BitIntZero()
        {
            Int32 intIn = (Int32)0x00000000;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 32);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(32);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void Test32BitIntOne()
        {
            unchecked
            {
                Int32 intIn = (Int32)0xFFFFFFFF;

                DatagramWriter writer = new DatagramWriter();
                writer.Write(intIn, 32);

                DatagramReader reader = new DatagramReader(writer.ToByteArray());
                Int32 intOut = reader.Read(32);

                Assert.IsEqualTo(intIn, intOut);
            }
        }

        public void Test16BitInt()
        {
            Int32 intIn = (Int32)0x00004321;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 16);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(16);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void Test8BitInt()
        {
            Int32 intIn = 0x00000021;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 8);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(8);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void Test4BitInt()
        {
            Int32 intIn = 0x0000005;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 4);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(4);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void Test2BitInt()
        {
            Int32 intIn = 0x00000002;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 2);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(2);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void Test1BitInt()
        {
            Int32 intIn = 0x00000001;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 1);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 intOut = reader.Read(1);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void TestByteOrder()
        {
            Int32 intIn = 1234567890;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(intIn, 32);

            Byte[] data = writer.ToByteArray();
            Int32 intTrans = System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data, 0));

            Assert.IsEqualTo(intIn, intTrans);

            DatagramReader reader = new DatagramReader(data);
            Int32 intOut = reader.Read(32);

            Assert.IsEqualTo(intIn, intOut);
        }

        public void TestAlignedBytes()
        {
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Byte[] bytesOut = reader.ReadBytesLeft();

            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestUnalignedBytes1()
        {
            Int32 bitCount = 1;
            Int32 bitsIn = 0x1;
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.Write(bitsIn, bitCount);
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 bitsOut = reader.Read(bitCount);
            Byte[] bytesOut = reader.ReadBytes(bytesIn.Length);

            Assert.IsEqualTo(bitsIn, bitsOut);
            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestUnalignedBytes3()
        {
            Int32 bitCount = 3;
            Int32 bitsIn = 0x5;
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.Write(bitsIn, bitCount);
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 bitsOut = reader.Read(bitCount);
            Byte[] bytesOut = reader.ReadBytes(bytesIn.Length);

            Assert.IsEqualTo(bitsIn, bitsOut);
            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestUnalignedBytes7()
        {
            Int32 bitCount = 7;
            Int32 bitsIn = 0x69;
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.Write(bitsIn, bitCount);
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 bitsOut = reader.Read(bitCount);
            Byte[] bytesOut = reader.ReadBytes(bytesIn.Length);

            Assert.IsEqualTo(bitsIn, bitsOut);
            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestBytesLeft()
        {
            Int32 bitCount = 8;
            Int32 bitsIn = 0xAA;
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.Write(bitsIn, bitCount);
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 bitsOut = reader.Read(bitCount);
            Byte[] bytesOut = reader.ReadBytesLeft();

            Assert.IsEqualTo(bitsIn, bitsOut);
            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestBytesLeftUnaligned()
        {
            Int32 bitCount = 7;
            Int32 bitsIn = 0x55;
            Byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes("Some aligned bytes");

            DatagramWriter writer = new DatagramWriter();
            writer.Write(bitsIn, bitCount);
            writer.WriteBytes(bytesIn);

            DatagramReader reader = new DatagramReader(writer.ToByteArray());
            Int32 bitsOut = reader.Read(bitCount);
            Byte[] bytesOut = reader.ReadBytesLeft();

            Assert.IsEqualTo(bitsIn, bitsOut);
            Assert.IsSequenceEqualTo(bytesIn, bytesOut);
        }

        public void TestGETRequestHeader()
        {
            Int32 versionIn = 1;
            Int32 versionSz = 2;
            Int32 typeIn = 0; // Confirmable
            Int32 typeSz = 2;
            Int32 optionCntIn = 1;
            Int32 optionCntSz = 4;
            Int32 codeIn = 1; // GET Request
            Int32 codeSz = 8;
            Int32 msgIdIn = 0x1234;
            Int32 msgIdSz = 16;

            DatagramWriter writer = new DatagramWriter();
            writer.Write(versionIn, versionSz);
            writer.Write(typeIn, typeSz);
            writer.Write(optionCntIn, optionCntSz);
            writer.Write(codeIn, codeSz);
            writer.Write(msgIdIn, msgIdSz);

            Byte[] data = writer.ToByteArray();
            Byte[] dataRef = { 0x41, 0x01, 0x12, 0x34 };

            Assert.IsSequenceEqualTo(dataRef, data);

            DatagramReader reader = new DatagramReader(data);
            Int32 versionOut = reader.Read(versionSz);
            Int32 typeOut = reader.Read(typeSz);
            Int32 optionCntOut = reader.Read(optionCntSz);
            Int32 codeOut = reader.Read(codeSz);
            Int32 msgIdOut = reader.Read(msgIdSz);

            Assert.IsEqualTo(versionIn, versionOut);
            Assert.IsEqualTo(typeIn, typeOut);
            Assert.IsEqualTo(optionCntIn, optionCntOut);
            Assert.IsEqualTo(codeIn, codeOut);
            Assert.IsEqualTo(msgIdIn, msgIdOut);
        }
    }
}
