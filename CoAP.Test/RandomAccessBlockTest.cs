using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using CoAP.Server;
using CoAP.Net;
using CoAP.Server.Resources;

namespace CoAP
{
    [TestClass]
    public class RandomAccessBlockTest
    {
        static readonly String TARGET = "test";
        static readonly String RESPONSE_PAYLOAD = "123456789_123456789_123456789_1234567890";

        Int32 _serverPort = 7777;
        CoapConfig _config = new CoapConfig();
        CoapServer _server;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            _config = new CoapConfig();
            _server = new CoapServer();
            CoAPEndPoint endpoint = new CoAPEndPoint(_serverPort, _config);
            _server.AddEndPoint(endpoint);
            _server.Add(new TestResource(TARGET));
            _server.Start();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void TestServer()
        {
            // We do not test for block 0 because the client is currently unable to
            // know if the user attempts to just retrieve block 0 or if he wants to
            // do early block negotiation with a specific size but actually wants to
            // retrieve all blocks.

            Int32[] blockOrder = { 2, 1, 3 };
            String[] expectations = {
                RESPONSE_PAYLOAD.Substring(32 /* until the end */),
                RESPONSE_PAYLOAD.Substring(16, 16),
                null // block is out of bounds
            };

            for (Int32 i = 0; i < blockOrder.Length; i++)
            {
                Int32 num = blockOrder[i];
                Console.WriteLine("Request block number " + num);
                Int32 szx = BlockOption.EncodeSZX(16);
                Request request = Request.NewGet();
                request.URI = new Uri("coap://localhost:" + _serverPort + "/" + TARGET);
                request.SetBlock2(szx, false, num);
                request.Send();
                Response response = request.WaitForResponse(1000);
                Assert.IsNotNull(response);
                Assert.AreEqual(expectations[i], response.PayloadString);
                Assert.IsTrue(response.HasOption(OptionType.Block2));
                Assert.AreEqual(num, response.Block2.NUM);
                Assert.AreEqual(szx, response.Block2.SZX);
            }
        }

        class TestResource : Resource
        {
            public TestResource(String name)
                : base(name)
            { }

            protected override void DoGet(CoapExchange exchange)
            {
                exchange.Respond(RESPONSE_PAYLOAD);
            }
        }
    }
}
