using System;

namespace CoAP.Test
{
    class OptionTest
    {
        //[ActiveTest]
        public void TestOption()
        {
            Byte[] data = System.Text.Encoding.UTF8.GetBytes("raw");
            Option opt = Option.Create(OptionType.ContentType, data);
            Assert.IsSequenceEqualTo(data, opt.RawValue);
            Assert.IsEqualTo(opt.Type, OptionType.ContentType);
        }

        public void TestIntOption()
        {
            Int32 oneByteValue = 255;
            Int32 twoByteValue = 256;

            Option opt1 = Option.Create(OptionType.ContentType, oneByteValue);
            Option opt2 = Option.Create(OptionType.ContentType, twoByteValue);

            Assert.IsEqualTo(opt1.Length, 1);
            Assert.IsEqualTo(opt2.Length, 2);
            Assert.IsEqualTo(opt1.IntValue, oneByteValue);
            Assert.IsEqualTo(opt2.IntValue, twoByteValue);
        }

        public void TestStringOption()
        {
            String s = "string";
            Option opt = Option.Create(OptionType.ContentType, s);
            Assert.IsEqualTo(s, opt.StringValue);
        }

        public void TestOptionEquality()
        {
            Int32 oneByteValue = 255;
            Int32 twoByteValue = 256;

            Option opt1 = Option.Create(OptionType.ContentType, oneByteValue);
            Option opt2 = Option.Create(OptionType.ContentType, twoByteValue);
            Option opt2_2 = Option.Create(OptionType.ContentType, twoByteValue);

            Assert.IsNotEqualTo(opt1, opt2);
            Assert.IsEqualTo(opt2, opt2_2);
        }

        public void TestEmptyToken()
        {
            Option t1 = Option.Create(OptionType.Token, new Byte[0]);
            Option t2 = Option.Create(OptionType.Token, new Byte[0]);
            Option t3 = Option.Create(OptionType.Token, "full");

            Assert.IsEqualTo(t1, t2);
            Assert.IsEqualTo(t1.Length, 0);
            Assert.IsNotEqualTo(t1, t3);
        }

        public void Test1ByteToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0xCD);
            Option t2 = Option.Create(OptionType.Token, 0xCD);
            Option t3 = Option.Create(OptionType.Token, 0xCE);

            Assert.IsEqualTo(t1, t2);
            Assert.IsEqualTo(t1.Length, 1);
            Assert.IsNotEqualTo(t1, t3);
        }

        public void Test2BytesToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0xABCD);
            Option t2 = Option.Create(OptionType.Token, 0xABCD);
            Option t3 = Option.Create(OptionType.Token, 0xABCE);

            Assert.IsEqualTo(t1, t2);
            Assert.IsEqualTo(t1.Length, 2);
            Assert.IsNotEqualTo(t1, t3);
        }

        public void Test4BytesToken()
        {
            Option t1 = Option.Create(OptionType.Token, 0x1234ABCD);
            Option t2 = Option.Create(OptionType.Token, 0x1234ABCD);
            Option t3 = Option.Create(OptionType.Token, 0x1234ABCE);

            Assert.IsEqualTo(t1, t2);
            Assert.IsEqualTo(t1.Length, 4);
            Assert.IsNotEqualTo(t1, t3);
        }
    }
}
