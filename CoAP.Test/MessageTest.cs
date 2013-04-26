using System;

namespace CoAP.Test
{
    class MessageTest
    {
#if COAPALL
        public ISpec Spec = CoAP.Spec.Draft08;
#endif

        public void TestMessage()
        {
            Message msg = new Message();

            msg.Code = Code.GET;
            msg.Type = MessageType.CON;
            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.IsEqualTo(msg.Code, convMsg.Code);
            Assert.IsEqualTo(msg.Type, convMsg.Type);
            Assert.IsEqualTo(msg.ID, convMsg.ID);
            Assert.IsEqualTo(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsSequenceEqualTo(msg.Payload, convMsg.Payload);
        }

        public void TestMessageWithOptions()
        {
            Message msg = new Message();

            msg.Code = Code.GET;
            msg.Type = MessageType.CON;
            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");
            msg.AddOption(Option.Create(OptionType.ContentType, "text/plain"));
            msg.AddOption(Option.Create(OptionType.MaxAge, 30));

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.IsEqualTo(msg.Code, convMsg.Code);
            Assert.IsEqualTo(msg.Type, convMsg.Type);
            Assert.IsEqualTo(msg.ID, convMsg.ID);
            Assert.IsEqualTo(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsSequenceEqualTo(msg.GetOptions(), convMsg.GetOptions());
            Assert.IsSequenceEqualTo(msg.Payload, convMsg.Payload);
        }

        public void TestMessageWithExtendedOption()
        {
            Message msg = new Message();

            msg.Code = Code.GET;
            msg.Type = MessageType.CON;
            msg.ID = 12345;
            msg.AddOption(Option.Create(Spec.GetOptionType(1), "a"));
            msg.AddOption(Option.Create((OptionType)197, "extend option"));
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.IsEqualTo(msg.Code, convMsg.Code);
            Assert.IsEqualTo(msg.Type, convMsg.Type);
            Assert.IsEqualTo(msg.ID, convMsg.ID);
            Assert.IsEqualTo(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsSequenceEqualTo(msg.GetOptions(), convMsg.GetOptions());
            Assert.IsSequenceEqualTo(msg.Payload, convMsg.Payload);

            Option extendOpt = convMsg.GetFirstOption((OptionType)197);
            Assert.IsNotNull(extendOpt);
            Assert.IsEqualTo(extendOpt.StringValue, "extend option");
        }
    }
}
