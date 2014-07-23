using System;
using System.Linq;
using System.Net;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace CoAP.Channel
{
    [TestClass]
    public class IPAddressTest
    {
        [TestMethod]
        public void TestMapBetweenIPv4AndIPv6()
        {
            for (Byte i = 0; i < Byte.MaxValue; i++)
            {
                IPAddress ipv4 = new IPAddress(new Byte[] { 10, 0, 0, i });
                IPAddress ipv6 = IPAddressExtensions.MapToIPv6(ipv4);
                Assert.IsTrue(IPAddressExtensions.IsIPv4MappedToIPv6(ipv6));

                IPAddress ipv4Mapped = IPAddressExtensions.MapToIPv4(ipv6);
                Assert.AreEqual(ipv4, ipv4Mapped);
            }
        }
    }
}
