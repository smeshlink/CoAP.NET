using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
using CoAP.Deduplication;
using CoAP.Server;
using CoAP.Net;
using CoAP.Server.Resources;

namespace CoAP
{
    [TestClass]
    public class MemoryLeakingHashMapTest
    {
        // Configuration for this test
        public const int TEST_EXCHANGE_LIFECYCLE = 247; // 0.247 seconds
        public const int TEST_SWEEP_DEDUPLICATOR_INTERVAL = 100; // 1 second
        public const int TEST_BLOCK_SIZE = 16; // 16 bytes

        public const int OBS_NOTIFICATION_INTERVALL = 50; // send one notification per 500 ms
        public const int HOW_MANY_NOTIFICATION_WE_WAIT_FOR = 3;

        // The names of the two resources of the server
        public const String PIGGY = "piggy";
        public const String SEPARATE = "separate";

        private IEndPoint _serverEndpoint;
        private IEndPoint _clientEndpoint;
        private EndpointSurveillant _serverSurveillant;
        private EndpointSurveillant _clientSurveillant;

        static String _currentRequestText;
        static String _currentResponseText;

        private CoapServer _server;
        private Int32 _serverPort;

        private Timer _timer;

        [TestInitialize]
        public void SetupServer()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            CoapConfig config = new CoapConfig();
            config.Deduplicator = "MarkAndSweep";
            config.MarkAndSweepInterval = TEST_SWEEP_DEDUPLICATOR_INTERVAL;
            config.ExchangeLifecycle = TEST_EXCHANGE_LIFECYCLE;
            config.MaxMessageSize = TEST_BLOCK_SIZE;
            config.DefaultBlockSize = TEST_BLOCK_SIZE;

            // Create the endpoint for the server and create surveillant
            _serverEndpoint = new CoAPEndPoint(config);
            _serverSurveillant = new EndpointSurveillant("server", _serverEndpoint);
            _clientEndpoint = new CoAPEndPoint(config);
            _clientEndpoint.Start();
            _clientSurveillant = new EndpointSurveillant("client", _clientEndpoint);

            // Create a server with two resources: one that sends piggy-backed
            // responses and one that sends separate responses

            _server = new CoapServer(config);
            _server.AddEndPoint(_serverEndpoint);
            TestResource piggyRes = new TestResource(PIGGY, Mode.PiggyBacked);
            TestResource separateRes = new TestResource(SEPARATE, Mode.Separate);
            _server.Add(piggyRes);
            _server.Add(separateRes);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)_serverEndpoint.LocalEndPoint).Port;

            _timer = new Timer(o =>
            {
                piggyRes.Fire();
                separateRes.Fire();
            }, null, 0, OBS_NOTIFICATION_INTERVALL);
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _timer.Dispose();
            _server.Dispose();
            _clientEndpoint.Dispose();
        }

        [TestMethod]
        public void TestServer()
        {
            //TestSimpleNONGet(UriFor(PIGGY));

            //TestSimpleGet(UriFor(PIGGY));
            //TestSimpleGet(UriFor(SEPARATE));

            //TestBlockwise(UriFor(PIGGY));
            //TestBlockwise(UriFor(SEPARATE));

            TestObserve(UriFor(PIGGY));
        }

        private void TestSimpleNONGet(Uri uri)
        {
            Console.WriteLine("Test simple NON GET to " + uri);

            Request request = Request.NewGet();
            request.URI = uri;
            request.Type = MessageType.NON;
            Response response = request.Send(_clientEndpoint).WaitForResponse(1000);

            Console.WriteLine("Client received response " + response.PayloadString + " with msg type " + response.Type);
            Assert.AreEqual(_currentResponseText, response.PayloadString);
            Assert.AreEqual(MessageType.NON, response.Type);

            _serverSurveillant.WaitUntilDeduplicatorShouldBeEmpty();
            _serverSurveillant.AssertMapsEmpty();
            _clientSurveillant.AssertMapsEmpty();
        }

        private void TestSimpleGet(Uri uri)
        {
            Console.WriteLine("Test simple GET to " + uri);

            CoapClient client = new CoapClient(uri);
            client.EndPoint = _clientEndpoint;

            Response response = client.Get();
            Console.WriteLine("Client received response " + response.PayloadString);
            Assert.AreEqual(_currentResponseText, response.PayloadString);

            _serverSurveillant.WaitUntilDeduplicatorShouldBeEmpty();
            _serverSurveillant.AssertMapsEmpty();
            _clientSurveillant.AssertMapsEmpty();
        }

        private void TestBlockwise(Uri uri)
        {
            Console.WriteLine("Test blockwise POST to " + uri);

            CoapClient client = new CoapClient(uri);
            client.EndPoint = _clientEndpoint;

            String ten = "123456789.";
            _currentRequestText = ten + ten + ten;
            Response response = client.Post(_currentRequestText, MediaType.TextPlain);
            Console.WriteLine("Client received response " + response.PayloadString);
            Assert.AreEqual(_currentResponseText, response.PayloadString);

            _serverSurveillant.WaitUntilDeduplicatorShouldBeEmpty();
            _serverSurveillant.AssertMapsEmpty();
            _clientSurveillant.AssertMapsEmpty();
        }

        private void TestObserve(Uri uri)
        {
            Console.WriteLine("Test observe relation with a reactive cancelation to " + uri);

            ManualResetEvent mre = new ManualResetEvent(false);

            CoapClient client = new CoapClient(uri);
            client.EndPoint = _clientEndpoint;

            Int32 notificationCounter = 0;
            CoapObserveRelation relation = null;
            relation = client.Observe(response =>
            {
                notificationCounter++;
                Console.WriteLine("Client received notification " + notificationCounter + ": " + response.PayloadString);

                if (notificationCounter == HOW_MANY_NOTIFICATION_WE_WAIT_FOR)
                {
                    Console.WriteLine("Client forgets observe relation to " + uri);
                    SpinWait.SpinUntil(() => relation != null);
                    relation.ProactiveCancel();
                }
                else if (notificationCounter == HOW_MANY_NOTIFICATION_WE_WAIT_FOR + 1)
                {
                    mre.Set();
                }
            }, reason => Assert.Fail(reason.ToString()));

            // Wait until we have received all the notifications and canceled the relation
            Thread.Sleep(HOW_MANY_NOTIFICATION_WE_WAIT_FOR * OBS_NOTIFICATION_INTERVALL + 1000);

            Boolean success = mre.WaitOne(100);
            Assert.IsTrue(success, "Client has not received all expected responses");

            _serverSurveillant.WaitUntilDeduplicatorShouldBeEmpty();
            _serverSurveillant.AssertMapsEmpty();
            _clientSurveillant.AssertMapsEmpty();
        }

        private Uri UriFor(String resourcePath)
        {
            return new Uri("coap://localhost:" + _serverPort + "/" + resourcePath);
        }

        enum Mode
        {
            PiggyBacked,
            Separate
        }

        class TestResource : Resource
        {
            private Mode _mode;
            private Int32 _status;

            public TestResource(String name, Mode mode)
                : base(name)
            {
                _mode = mode;
                Observable = true;
            }

            public void Fire()
            {
                _status++;
                Changed();
            }

            protected override void DoGet(CoapExchange exchange)
            {
                if (_mode == Mode.Separate)
                    exchange.Accept();
                _currentResponseText = "hello get " + _status;
                exchange.Respond(_currentResponseText);
            }

            protected override void DoPost(CoapExchange exchange)
            {
                Assert.AreEqual(_currentRequestText, exchange.Request.PayloadString);
                if (_mode == Mode.Separate)
                    exchange.Accept();

                Console.WriteLine("TestResource " + Name + " received POST message: " + exchange.Request.PayloadString);
                String ten = "123456789.";
                _currentResponseText = "hello post " + _status + ten + ten + ten;
                exchange.Respond(StatusCode.Created, _currentResponseText);
            }

            protected override void DoPut(CoapExchange exchange)
            {
                Assert.AreEqual(_currentRequestText, exchange.Request.PayloadString);
                exchange.Accept();
                _currentResponseText = "";
                exchange.Respond(StatusCode.Changed);
            }

            protected override void DoDelete(CoapExchange exchange)
            {
                _currentResponseText = "";
                exchange.Respond(StatusCode.Deleted);
            }
        }
    }

    class EndpointSurveillant
    {
        private String _name;
        private Int32 _exchangeLifecycle;
        private Int32 _sweepDuplicatorInterval;

        private IDictionary<Exchange.KeyID, Exchange> _exchangesByID;       // Outgoing to match with inc ACK/RST
        private IDictionary<Exchange.KeyToken, Exchange> _exchangesByToken; // Outgoing to match with inc responses
        private IDictionary<Exchange.KeyUri, Exchange> _ongoingExchanges;   // for blockwise
        private IDictionary<Exchange.KeyID, Exchange> _incommingMessages;   // for deduplication

        public EndpointSurveillant(String name, IEndPoint endpoint)
        {
            ICoapConfig config = endpoint.Config;
            _exchangeLifecycle = config.ExchangeLifecycle;
            _sweepDuplicatorInterval = (Int32)config.MarkAndSweepInterval;
            _name = name;

            ExtractMaps(endpoint);
        }

        public void WaitUntilDeduplicatorShouldBeEmpty()
        {
            int time = _exchangeLifecycle + _sweepDuplicatorInterval + 100;
            Console.WriteLine("Wait until deduplicator should be empty (" + time / 1000f + " seconds)");
            Thread.Sleep(time);
        }

        public void AssertMapsEmpty()
        {
            Assert.AreEqual(0, _exchangesByID.Count);
            Assert.AreEqual(0, _exchangesByToken.Count);
            Assert.AreEqual(0, _ongoingExchanges.Count);
            Assert.AreEqual(0, _incommingMessages.Count);
        }

        private void ExtractMaps(IEndPoint endpoint)
        {
            IMatcher matcher = ExtractField<IMatcher>(endpoint, "_matcher");

            _exchangesByID = ExtractField<IDictionary<Exchange.KeyID, Exchange>>(matcher, "_exchangesByID");
            _exchangesByToken = ExtractField<IDictionary<Exchange.KeyToken, Exchange>>(matcher, "_exchangesByToken");
            _ongoingExchanges = ExtractField<IDictionary<Exchange.KeyUri, Exchange>>(matcher, "_ongoingExchanges");

            IDeduplicator deduplicator = ExtractField<IDeduplicator>(matcher, "_deduplicator");
            _incommingMessages = ExtractField<IDictionary<Exchange.KeyID, Exchange>>(deduplicator, "_incommingMessages");
        }

        private static T ExtractField<T>(Object obj, String name)
        {
            FieldInfo fi = obj.GetType().GetField(name, BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)fi.GetValue(obj);
        }
    }
}
