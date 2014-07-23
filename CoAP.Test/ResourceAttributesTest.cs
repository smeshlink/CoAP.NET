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
    public class ResourceAttributesTest
    {
        IResource _root;

        [TestInitialize]
        public void Setup()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
            _root = new Resource(String.Empty);
            Resource sensors = new Resource("sensors");
            Resource temp = new Resource("temp");
            Resource light = new Resource("light");
            _root.Add(sensors);
            sensors.Add(light);
            sensors.Add(temp);

            sensors.Attributes.Title = "Sensor Index";
            temp.Attributes.AddResourceType("temperature-c");
            temp.Attributes.AddInterfaceDescription("sensor");
            temp.Attributes.Add("foo");
            temp.Attributes.Add("bar", "one");
            temp.Attributes.Add("bar", "two");
            light.Attributes.AddResourceType("light-lux");
            light.Attributes.AddInterfaceDescription("sensor");
        }

        [TestMethod]
        public void TestDiscovery()
        {
            String[] expected = new String[] {
                "</sensors>;title=\"Sensor Index\"," +
                    "</sensors/temp>;bar=\"one two\";rt=\"temperature-c\";foo;if=\"sensor\"," +
                    "</sensors/light>;rt=\"light-lux\";if=\"sensor\"",
                "</sensors>;title=\"Sensor Index\"," +
                    "</sensors/temp>;if=\"sensor\";bar=\"one two\";foo;rt=\"temperature-c\"," +
                    "</sensors/light>;if=\"sensor\";rt=\"light-lux\""
            };
            DiscoveryResource discovery = new DiscoveryResource(_root);
            String serialized = LinkFormat.Serialize(_root, null);
            CollectionAssert.Contains(expected, serialized);

            serialized = LinkFormat.Serialize(_root, new List<String>());
            CollectionAssert.Contains(expected, serialized);
        }

        [TestMethod]
        public void TestDiscoveryFiltering()
        {
            String[] expected = new String[] {
                "</sensors/light>;rt=\"light-lux\";if=\"sensor\"",
                "</sensors/light>;if=\"sensor\";rt=\"light-lux\""
            };

            Request request = Request.NewGet();
            request.SetUri("/.well-known/core?rt=light-lux");

            DiscoveryResource discovery = new DiscoveryResource(_root);
            String serialized = LinkFormat.Serialize(_root, request.UriQueries);
            CollectionAssert.Contains(expected, serialized);
        }
    }
}
