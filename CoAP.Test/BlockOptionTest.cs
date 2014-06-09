using System;
using System.Linq;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace CoAP
{
    [TestClass]
    public class BlockOptionTest
    {
        [TestMethod]
        public void TestGetValue()
        {
            Assert.IsTrue(ToBytes(0, false, 0).SequenceEqual(b()));
            Assert.IsTrue(ToBytes(0, false, 1).SequenceEqual(b(0x10)));
            Assert.IsTrue(ToBytes(0, false, 15).SequenceEqual(b(0xf0)));
            Assert.IsTrue(ToBytes(0, false, 16).SequenceEqual(b(0x01, 0x00)));
            Assert.IsTrue(ToBytes(0, false, 79).SequenceEqual(b(0x04, 0xf0)));
            Assert.IsTrue(ToBytes(0, false, 113).SequenceEqual(b(0x07, 0x10)));
            Assert.IsTrue(ToBytes(0, false, 26387).SequenceEqual(b(0x06, 0x71, 0x30)));
            Assert.IsTrue(ToBytes(0, false, 1048575).SequenceEqual(b(0xff, 0xff, 0xf0)));
            Assert.IsTrue(ToBytes(7, false, 1048575).SequenceEqual(b(0xff, 0xff, 0xf7)));
            Assert.IsTrue(ToBytes(7, true, 1048575).SequenceEqual(b(0xff, 0xff, 0xff)));
        }

        [TestMethod]
        public void TestCombined()
        {
            TestCombined(0, false, 0);
            TestCombined(0, false, 1);
            TestCombined(0, false, 15);
            TestCombined(0, false, 16);
            TestCombined(0, false, 79);
            TestCombined(0, false, 113);
            TestCombined(0, false, 26387);
            TestCombined(0, false, 1048575);
            TestCombined(7, false, 1048575);
            TestCombined(7, true, 1048575);
        }

        /// <summary>
        /// Converts a BlockOption with the specified parameters to a byte array and
        /// back and checks that the result is the same as the original.
        /// </summary>
        private void TestCombined(Int32 szx, Boolean m, Int32 num)
        {
            BlockOption block = new BlockOption(OptionType.Block1, num, szx, m);
            BlockOption copy = new BlockOption(OptionType.Block1);
            copy.RawValue = block.RawValue;
            Assert.AreEqual(block.SZX, copy.SZX);
            Assert.AreEqual(block.M, copy.M);
            Assert.AreEqual(block.NUM, copy.NUM);
        }

        /// <summary>
        /// Helper function that creates a BlockOption with the specified parameters
        /// and serializes them to a byte array.
        /// </summary>
        private Byte[] ToBytes(Int32 szx, Boolean m, Int32 num)
        {
            Byte[] bytes = new BlockOption(OptionType.Block1, num, szx, m).RawValue;
            return bytes;
        }

        /// <summary>
        /// Helper function that converts an int array to a byte array.
        /// </summary>
        private Byte[] b(params Int32[] a)
        {
            Byte[] ret = new Byte[a.Length];
            for (Int32 i = 0; i < a.Length; i++)
                ret[i] = (Byte)a[i];
            return ret;
        }
    }
}
