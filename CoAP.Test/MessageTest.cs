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
    public class MessageTest
    {
#if COAPALL
        [TestMethod]
        public void TestDraft03()
        {
            TestMessage(CoAP.Spec.Draft03);
            TestMessageWithOptions(CoAP.Spec.Draft03);
            TestMessageWithExtendedOption(CoAP.Spec.Draft03);
        }

        [TestMethod]
        public void TestDraft08()
        {
            TestMessage(CoAP.Spec.Draft08);
            TestMessageWithOptions(CoAP.Spec.Draft08);
            TestMessageWithExtendedOption(CoAP.Spec.Draft08);
        }

        [TestMethod]
        public void TestDraft12()
        {
            TestMessage(CoAP.Spec.Draft12);
            TestMessageWithOptions(CoAP.Spec.Draft12);
            TestMessageWithExtendedOption(CoAP.Spec.Draft12);
        }

        [TestMethod]
        public void TestDraft13()
        {
            TestMessage(CoAP.Spec.Draft13);
            TestMessageWithOptions(CoAP.Spec.Draft13);
            TestMessageWithExtendedOption(CoAP.Spec.Draft13);
        }
#endif

#if COAPALL
        public void TestMessage(ISpec Spec)
#else
        [TestMethod]
        public void TestMessage()
#endif
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));
        }

#if COAPALL
        public void TestMessageWithOptions(ISpec Spec)
#else
        [TestMethod]
        public void TestMessageWithOptions()
#endif
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");
            msg.AddOption(Option.Create(OptionType.ContentType, "text/plain"));
            msg.AddOption(Option.Create(OptionType.MaxAge, 30));

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsTrue(msg.GetOptions().SequenceEqual(convMsg.GetOptions()));
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));
        }

#if COAPALL
        public void TestMessageWithExtendedOption(ISpec Spec)
#else
        [TestMethod]
        public void TestMessageWithExtendedOption()
#endif
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.AddOption(Option.Create((OptionType)12, "a"));
            msg.AddOption(Option.Create((OptionType)197, "extend option"));
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            Byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptionCount(), convMsg.GetOptionCount());
            Assert.IsTrue(msg.GetOptions().SequenceEqual(convMsg.GetOptions()));
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));

            Option extendOpt = convMsg.GetFirstOption((OptionType)197);
            Assert.IsNotNull(extendOpt);
            Assert.AreEqual(extendOpt.StringValue, "extend option");
        }
    }
}
