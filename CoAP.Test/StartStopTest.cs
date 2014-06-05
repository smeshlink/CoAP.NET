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
using System.Net.Sockets;
using System.Net;

namespace CoAP
{
    [TestClass]
    public class StartStopTest
    {
        static readonly String SERVER_1_RESPONSE = "This is server one";
        static readonly String SERVER_2_RESPONSE = "This is server two";

        private CoapServer _server1, _server2;
        private Int32 _serverPort = 7777;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogManager.LogLevel.Fatal;

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                _serverPort = ((IPEndPoint)socket.LocalEndPoint).Port;
                socket.Close();
            }

            _server1 = new CoapServer(_serverPort);
            _server1.Add(new TestResource("ress", SERVER_1_RESPONSE));

            _server2 = new CoapServer(_serverPort);
            _server2.Add(new TestResource("ress", SERVER_2_RESPONSE));
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            if (_server1 != null)
                _server1.Dispose();
            if (_server2 != null)
                _server2.Dispose();
        }

        [TestMethod]
        public void TestStartStop()
        {
            _server1.Start();
            SendRequestAndExpect(SERVER_1_RESPONSE);

            for (int i = 0; i < 3; i++)
            {
                _server1.Stop();
                Thread.Sleep(100); // sometimes Travis does not free the port immediately
                _server2.Start();
                SendRequestAndExpect(SERVER_2_RESPONSE);

                _server2.Stop();
                Thread.Sleep(100); // sometimes Travis does not free the port immediately
                _server1.Start();
                SendRequestAndExpect(SERVER_1_RESPONSE);
            }

            _server1.Stop();
        }

        private void SendRequestAndExpect(String expected)
        {
            Thread.Sleep(100);
            Request request = Request.NewGet();
            request.SetUri("localhost:" + _serverPort + "/ress");
            String response = request.Send().WaitForResponse(1000).PayloadString;
            Assert.AreEqual(expected, response);
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
