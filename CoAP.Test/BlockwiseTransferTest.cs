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

namespace CoAP
{
    [TestClass]
    public class BlockwiseTransferTest
    {
        static readonly String SHORT_POST_REQUEST = "<Short request>";
        static readonly String LONG_POST_REQUEST = "<Long request 1x2x3x4x5x>".Replace("x", "ABCDEFGHIJKLMNOPQRSTUVWXYZ ");
        static readonly String SHORT_POST_RESPONSE = "<Short response>";
        static readonly String LONG_POST_RESPONSE = "<Long response 1x2x3x4x5x>".Replace("x", "ABCDEFGHIJKLMNOPQRSTUVWXYZ ");
        static readonly String SHORT_GET_RESPONSE = SHORT_POST_RESPONSE.ToLower();
        static readonly String LONG_GET_RESPONSE = LONG_POST_RESPONSE.ToLower();

        Int32 _serverPort = 7777;
        CoapConfig _config = new CoapConfig();
        CoapServer _server;
        IEndPoint _clientEndpoint;

        Boolean request_short = true;
        Boolean respond_short = true;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogManager.LogLevel.Fatal;
            _config = new CoapConfig();
            _config.DefaultBlockSize = 32;
            _config.MaxMessageSize = 32;
            CreateServer();
            _clientEndpoint = new CoAPEndPoint(_config);
            _clientEndpoint.Start();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
            _clientEndpoint.Dispose();
        }

        [TestMethod]
        public void Test_POST_short_short()
        {
            request_short = true;
            respond_short = true;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_long_short()
        {
            request_short = false;
            respond_short = true;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_short_long()
        {
            request_short = true;
            respond_short = false;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_POST_long_long()
        {
            request_short = false;
            respond_short = false;
            ExecutePOSTRequest();
        }

        [TestMethod]
        public void Test_GET_short()
        {
            respond_short = true;
            ExecuteGETRequest();
        }

        [TestMethod]
        public void Test_GET_long()
        {
            respond_short = false;
            ExecuteGETRequest();
        }

        private void ExecuteGETRequest()
        {
            String payload = "nothing";
            try
            {
                Request request = Request.NewGet();
                request.Destination = new IPEndPoint(IPAddress.Loopback, _serverPort);
                request.Send(_clientEndpoint);

                // receive response and check
                Response response = request.WaitForResponse(1000);

                Assert.IsNotNull(response);
                payload = response.PayloadString;
                if (respond_short)
                    Assert.AreEqual(SHORT_GET_RESPONSE, payload);
                else
                    Assert.AreEqual(LONG_GET_RESPONSE, payload);
            }
            finally
            {
                Thread.Sleep(100); // Quickly wait until last ACKs arrive
            }
        }

        private void ExecutePOSTRequest()
        {
            String payload = "--no payload--";
            try
            {
                Request request = new Request(Method.POST);
                request.SetUri("coap://localhost:" + _serverPort + "/" + request_short + respond_short);
                if (request_short)
                    request.SetPayload(SHORT_POST_REQUEST);
                else
                    request.SetPayload(LONG_POST_REQUEST);
                request.Send(_clientEndpoint);

                // receive response and check
                Response response = request.WaitForResponse(1000);

                Assert.IsNotNull(response);
                payload = response.PayloadString;

                if (respond_short)
                    Assert.AreEqual(SHORT_POST_RESPONSE, payload);
                else 
                    Assert.AreEqual(LONG_POST_RESPONSE, payload);
            }
            finally
            {
                Thread.Sleep(100); // Quickly wait until last ACKs arrive
            }
        }

        private void CreateServer()
        {
            _server = new CoapServer();
            CoAPEndPoint endpoint = new CoAPEndPoint(_serverPort, _config);
            _server.AddEndPoint(endpoint);
            _server.MessageDeliverer = new MessageDeliverer(this);
            _server.Start();
        }

        class MessageDeliverer : IMessageDeliverer
        {
            readonly BlockwiseTransferTest _test;

            public MessageDeliverer(BlockwiseTransferTest test)
            {
                _test = test;
            }

            public void DeliverRequest(Exchange exchange)
            {
                if (exchange.Request.Method == Method.GET)
                    ProcessGET(exchange);
                else
                    ProcessPOST(exchange);
            }

            public void DeliverResponse(Exchange exchange, Response response)
            { }

            private void ProcessGET(Exchange exchange)
            {
                Response response = new Response(StatusCode.Content);
                if (_test.respond_short)
                    response.SetPayload(SHORT_GET_RESPONSE);
                else response.SetPayload(LONG_GET_RESPONSE);
                exchange.SendResponse(response);
            }

            private void ProcessPOST(Exchange exchange)
            {
                String payload = exchange.Request.PayloadString;
                if (_test.request_short)
                    Assert.AreEqual(payload, SHORT_POST_REQUEST);
                else
                    Assert.AreEqual(payload, LONG_POST_REQUEST);

                Response response = new Response(StatusCode.Changed);
                if (_test.respond_short)
                    response.SetPayload(SHORT_POST_RESPONSE);
                else
                    response.SetPayload(LONG_POST_RESPONSE);
                exchange.SendResponse(response);
            }
        }
    }
}
