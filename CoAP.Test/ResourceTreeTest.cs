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
    public class ResourceTreeTest
    {
        static readonly String RES_A = "A";
        static readonly String RES_AA = "AA";

        static readonly String NAME_1 = "first";
        static readonly String NAME_2 = "second";
        static readonly String PAYLOAD = "It is freezing";

        static readonly String CHILD = "child";
        static readonly String CHILD_PAYLOAD = "It is too cold";

        Int32 _serverPort;
        CoapServer _server;
        Resource _resource;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            CreateServer();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void TestNameChange()
        {
            String baseUri = "coap://localhost:" + _serverPort + "/" + RES_A + "/" + RES_AA + "/";

            // First check that we reach the resource
            String resp1 = Request.NewGet().SetUri(baseUri + NAME_1).Send().WaitForResponse(100).PayloadString;
            Assert.AreEqual(PAYLOAD, resp1);

            // Check that the child of 'first' is also reachable
            String resp2 = Request.NewGet().SetUri(baseUri + NAME_1 + "/" + CHILD).Send().WaitForResponse(100).PayloadString;
            Assert.AreEqual(CHILD_PAYLOAD, resp2);

            // change the name to 'second'
            _resource.Name = NAME_2;

            // Check that the resource reacts
            String resp3 = Request.NewGet().SetUri(baseUri + NAME_2).Send().WaitForResponse(100).PayloadString;
            Assert.AreEqual(PAYLOAD, resp3);

            // Check that the child of (now) 'second' is also reachable
            String resp4 = Request.NewGet().SetUri(baseUri + NAME_2 + "/" + CHILD).Send().WaitForResponse(100).PayloadString;
            Assert.AreEqual(CHILD_PAYLOAD, resp4);

            // Check that the resource is not found at the old URI
            StatusCode code1 = Request.NewGet().SetUri(baseUri + NAME_1).Send().WaitForResponse(100).StatusCode;
            Assert.AreEqual(StatusCode.NotFound, code1);

            // Check that the child of (now) 'second' is not reachable under 'first'
            StatusCode code2 = Request.NewGet().SetUri(baseUri + NAME_1 + "/" + CHILD).Send().WaitForResponse(100).StatusCode;
            Assert.AreEqual(StatusCode.NotFound, code2);
        }

        private void CreateServer()
        {
            CoAPEndPoint endpoint = new CoAPEndPoint(0);

            _resource = new TestResource(NAME_1, PAYLOAD);
            _server = new CoapServer();
            _server
                .Add(new Resource(RES_A)
                    .Add(new Resource(RES_AA)
                        .Add(_resource
                            .Add(new TestResource(CHILD, CHILD_PAYLOAD)))));

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
        }

        class TestResource : Resource
        {
            String _payload;

            public TestResource(String name, String payload)
                : base(name)
            {
                _payload = payload;
            }

            protected override void DoGet(CoapExchange exchange)
            {
                exchange.Respond(_payload);
            }
        }
    }
}
