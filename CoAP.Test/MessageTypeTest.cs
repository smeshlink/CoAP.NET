using System;
using System.Collections.Generic;
using System.Linq;
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
using CoAP.Net;
using CoAP.Server;
using CoAP.Server.Resources;

namespace CoAP
{
    [TestClass]
    public class MessageTypeTest
    {
        static readonly String SERVER_RESPONSE = "server responds hi";
        static readonly String ACC_RESOURCE = "acc-res";
        static readonly String NO_ACC_RESOURCE = "no-acc-res";

        Int32 _serverPort;
        CoapServer _server;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            CoAPEndPoint endpoint = new CoAPEndPoint();
            _server = new CoapServer();
            _server.Add(new AccResource());
            _server.Add(new NoAccResource());
            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void TestNonConfirmable()
        {
            // send request
            Request req2acc = new Request(Method.POST, false);
            req2acc.SetUri("localhost:" + _serverPort + "/" + ACC_RESOURCE);
            req2acc.SetPayload("client says hi");
            req2acc.Send();

            // receive response and check
            Response response = req2acc.WaitForResponse(100);
            Assert.IsNotNull(response);
            Assert.AreEqual(response.PayloadString, SERVER_RESPONSE);
            Assert.AreEqual(response.Type, MessageType.NON);

            Request req2noacc = new Request(Method.POST, false);
            req2noacc.SetUri("coap://localhost:" + _serverPort + "/" + NO_ACC_RESOURCE);
            req2noacc.SetPayload("client says hi");
            req2noacc.Send();

            // receive response and check
            response = req2noacc.WaitForResponse(100);
            Assert.IsNotNull(response);
            Assert.AreEqual(response.PayloadString, SERVER_RESPONSE);
            Assert.AreEqual(response.Type, MessageType.NON);
        }

        [TestMethod]
        public void TestConfirmable()
        {
            // send request
            Request req2acc = new Request(Method.POST, true);
            req2acc.SetUri("localhost:" + _serverPort + "/" + ACC_RESOURCE);
            req2acc.SetPayload("client says hi");
            req2acc.Send();

            // receive response and check
            Response response = req2acc.WaitForResponse(100);
            Assert.IsNotNull(response);
            Assert.AreEqual(response.PayloadString, SERVER_RESPONSE);
            Assert.AreEqual(response.Type, MessageType.CON);

            Request req2noacc = new Request(Method.POST, true);
            req2noacc.SetUri("coap://localhost:" + _serverPort + "/" + NO_ACC_RESOURCE);
            req2noacc.SetPayload("client says hi");
            req2noacc.Send();

            // receive response and check
            response = req2noacc.WaitForResponse(100);
            Assert.IsNotNull(response);
            Assert.AreEqual(response.PayloadString, SERVER_RESPONSE);
            Assert.AreEqual(response.Type, MessageType.ACK);
        }

        class AccResource : Resource
        {
            public AccResource()
                : base(ACC_RESOURCE)
            { }

            protected override void DoPost(CoapExchange exchange)
            {
                exchange.Accept();
                exchange.Respond(SERVER_RESPONSE);
            }
        }

        class NoAccResource : Resource
        {
            public NoAccResource()
                : base(NO_ACC_RESOURCE)
            { }

            protected override void DoPost(CoapExchange exchange)
            {
                exchange.Respond(SERVER_RESPONSE);
            }
        }
    }
}
