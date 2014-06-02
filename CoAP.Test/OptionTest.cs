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
    public class OptionTest
    {
        [TestMethod]
        public void TestOption()
        {
            Byte[] data = System.Text.Encoding.UTF8.GetBytes("raw");
            Option opt = Option.Create(OptionType.ContentType, data);
            Assert.IsTrue(data.SequenceEqual(opt.RawValue));
            Assert.AreEqual(opt.Type, OptionType.ContentType);
        }

        [TestMethod]
        public void TestIntOption()
        {
            Int32 oneByteValue = 255;
            Int32 twoByteValue = 256;

            Option opt1 = Option.Create(OptionType.ContentType, oneByteValue);
            Option opt2 = Option.Create(OptionType.ContentType, twoByteValue);

            Assert.AreEqual(opt1.Length, 1);
            Assert.AreEqual(opt2.Length, 2);
            Assert.AreEqual(opt1.IntValue, oneByteValue);
            Assert.AreEqual(opt2.IntValue, twoByteValue);
        }

        [TestMethod]
        public void TestStringOption()
        {
            String s = "string";
            Option opt = Option.Create(OptionType.ContentType, s);
            Assert.AreEqual(s, opt.StringValue);
        }

        [TestMethod]
        public void TestOptionEquality()
        {
            Int32 oneByteValue = 255;
            Int32 twoByteValue = 256;

            Option opt1 = Option.Create(OptionType.ContentType, oneByteValue);
            Option opt2 = Option.Create(OptionType.ContentType, twoByteValue);
            Option opt2_2 = Option.Create(OptionType.ContentType, twoByteValue);

            Assert.AreNotEqual(opt1, opt2);
            Assert.AreEqual(opt2, opt2_2);
        }

        [TestMethod]
        public void TestEmptyToken()
        {
            Option t1 = Option.Create(OptionType.Token, new Byte[0]);
            Option t2 = Option.Create(OptionType.Token, new Byte[0]);
            Option t3 = Option.Create(OptionType.Token, "full");

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1.Length, 0);
            Assert.AreNotEqual(t1, t3);
        }

        [TestMethod]
        public void Test1ByteToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0xCD);
            Option t2 = Option.Create(OptionType.Token, 0xCD);
            Option t3 = Option.Create(OptionType.Token, 0xCE);

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1.Length, 1);
            Assert.AreNotEqual(t1, t3);
        }

        [TestMethod]
        public void Test2BytesToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0xABCD);
            Option t2 = Option.Create(OptionType.Token, 0xABCD);
            Option t3 = Option.Create(OptionType.Token, 0xABCE);

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1.Length, 2);
            Assert.AreNotEqual(t1, t3);
        }

        [TestMethod]
        public void Test4BytesToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0x1234ABCD);
            Option t2 = Option.Create(OptionType.Token, 0x1234ABCD);
            Option t3 = Option.Create(OptionType.Token, 0x1234ABCE);

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1.Length, 4);
            Assert.AreNotEqual(t1, t3);
        }

        [TestMethod]
        public void TestSetValue()
        {
            Option option = Option.Create(OptionType.Reserved);

            option.RawValue= new Byte[4];
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[4]));

            option.RawValue = new Byte[] { 69, 152, 35, 55, 152, 116, 35, 152 };
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 69, 152, 35, 55, 152, 116, 35, 152 }));
        }

        [TestMethod]
        public void TestSetStringValue()
        {
            Option option = Option.Create(OptionType.Reserved);

            option.StringValue = "";
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[0]));

            option.StringValue = "CoAP.NET";
            Assert.IsTrue(option.RawValue.SequenceEqual(System.Text.Encoding.UTF8.GetBytes("CoAP.NET")));
        }

        [TestMethod]
        public void TestSetIntegerValue()
        {
            Option option = Option.Create(OptionType.Reserved);

            option.IntValue = 0;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[0]));

            option.IntValue = 11;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 11 }));

            option.IntValue = 255;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { (Byte)255 }));

            option.IntValue = 256;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 0 }));

            option.IntValue = 18273;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 71, 97 }));

            option.IntValue = 1 << 16;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 0, 0 }));

            option.IntValue = 23984773;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 109, (Byte)250, (Byte)133 }));

            unchecked
            {
                option.IntValue = (Int32)0xFFFFFFFF;
                Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { (Byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xFF }));
            }
        }

        [TestMethod]
        public void TestSetLongValue()
        {
            Option option = Option.Create(OptionType.Reserved);

            option.LongValue = 0;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[0]));

            option.LongValue = 11;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 11 }));

            option.LongValue = 255;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { (Byte)255 }));

            option.LongValue = 256;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 0 }));

            option.LongValue = 18273;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 71, 97 }));

            option.LongValue = 1 << 16;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 0, 0 }));

            option.LongValue = 23984773;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 1, 109, (Byte)250, (Byte)133 }));

            option.LongValue = 0xFFFFFFFFL;
            Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { (Byte)0xFF, (Byte)0xFF, (Byte)0xFF, (Byte)0xFF }));

            unchecked
            {
                option.LongValue = (Int64)0x9823749837239845L;
                Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] { 152, 35, 116, 152, 55, 35, 152, 69 }));

                option.LongValue = (Int64)0xFFFFFFFFFFFFFFFFL;
                Assert.IsTrue(option.RawValue.SequenceEqual(new Byte[] {
                    (Byte) 0xFF, (Byte) 0xFF, (Byte) 0xFF, (Byte) 0xFF,
			        (Byte) 0xFF, (Byte) 0xFF, (Byte) 0xFF, (Byte) 0xFF}));
            }
        }
    }
}
