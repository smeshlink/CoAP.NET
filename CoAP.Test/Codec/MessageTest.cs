using System;
using System.Linq;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace CoAP.Codec
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
            //TestRequestParsing(CoAP.Spec.Draft03);
            //TestResponseParsing(CoAP.Spec.Draft03);
        }

        [TestMethod]
        public void TestDraft08()
        {
            TestMessage(CoAP.Spec.Draft08);
            TestMessageWithOptions(CoAP.Spec.Draft08);
            TestMessageWithExtendedOption(CoAP.Spec.Draft08);
            //TestRequestParsing(CoAP.Spec.Draft08);
            //TestResponseParsing(CoAP.Spec.Draft08);
        }

        [TestMethod]
        public void TestDraft12()
        {
            TestMessage(CoAP.Spec.Draft12);
            TestMessageWithOptions(CoAP.Spec.Draft12);
            TestMessageWithExtendedOption(CoAP.Spec.Draft12);
            TestRequestParsing(CoAP.Spec.Draft12);
            TestResponseParsing(CoAP.Spec.Draft12);
        }

        [TestMethod]
        public void TestDraft13()
        {
            TestMessage(CoAP.Spec.Draft13);
            TestMessageWithOptions(CoAP.Spec.Draft13);
            TestMessageWithExtendedOption(CoAP.Spec.Draft13);
            TestRequestParsing(CoAP.Spec.Draft13);
            TestResponseParsing(CoAP.Spec.Draft13);
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
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
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
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
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
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
            Assert.IsTrue(msg.GetOptions().SequenceEqual(convMsg.GetOptions()));
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));

            Option extendOpt = convMsg.GetFirstOption((OptionType)197);
            Assert.IsNotNull(extendOpt);
            Assert.AreEqual(extendOpt.StringValue, "extend option");
        }

#if COAPALL
        public void TestRequestParsing(ISpec Spec)
#else
        [TestMethod]
        public void TestRequestParsing()
#endif
        {
            Request request = new Request(Method.POST, false);
            request.ID = 7;
            request.Token = new Byte[] { 11, 82, 165, 77, 3 };
            request.AddIfMatch(new Byte[] { 34, 239 })
                .AddIfMatch(new Byte[] { 88, 12, 254, 157, 5 });
            request.ContentType = 40;
            request.Accept = 40;

            Byte[] bytes = Spec.NewMessageEncoder().Encode(request);
            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsRequest);

            Request result = decoder.DecodeRequest();
            Assert.AreEqual(request.ID, result.ID);
            Assert.IsTrue(request.Token.SequenceEqual(result.Token));
            Assert.IsTrue(request.GetOptions().SequenceEqual(result.GetOptions()));
        }

#if COAPALL
        public void TestResponseParsing(ISpec Spec)
#else
        [TestMethod]
        public void TestResponseParsing()
#endif
        {
            Response response = new Response(StatusCode.Content);
            response.Type = MessageType.NON;
            response.ID = 9;
            response.Token = new Byte[] { 22, 255, 0, 78, 100, 22 };
            response.AddETag(new Byte[] { 1, 0, 0, 0, 0, 1 })
                                .AddLocationPath("/one/two/three/four/five/six/seven/eight/nine/ten")
                                .AddOption(Option.Create((OptionType)57453, "Arbitrary".GetHashCode()))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary1"))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary2"))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary3"));

            Byte[] bytes = Spec.NewMessageEncoder().Encode(response);

            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsResponse);

            Response result = decoder.DecodeResponse();
            Assert.AreEqual(response.ID, result.ID);
            Assert.IsTrue(response.Token.SequenceEqual(result.Token));
            Assert.IsTrue(response.GetOptions().SequenceEqual(result.GetOptions()));
        }
    }
}
