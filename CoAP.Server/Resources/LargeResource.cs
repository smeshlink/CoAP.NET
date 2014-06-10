using System;
using System.Text;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    class LargeResource : Resource
    {
        static String payload;

        static LargeResource()
        {
            payload = new StringBuilder()
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 1 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 2 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 3 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 4 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 5 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 6 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 7 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .Append("/-------------------------------------------------------------\\\r\n")
                .Append("|                 RESOURCE BLOCK NO. 8 OF 8                   |\r\n")
                .Append("|               [each line contains 64 bytes]                 |\r\n")
                .Append("\\-------------------------------------------------------------/\r\n")
                .ToString();
        }

        public LargeResource(String name)
            : base(name)
        {
            Attributes.Title = "This is a large resource for testing block-wise transfer";
            Attributes.AddResourceType("BlockWiseTransferTester");
        }

        protected override void DoGet(CoapExchange exchange)
        {
            exchange.Respond(payload);
        }

        protected override void DoPost(CoAP.Server.Resources.CoapExchange exchange)
        {
            exchange.Respond(payload);
        }

        protected override void DoPut(CoAP.Server.Resources.CoapExchange exchange)
        {
            exchange.Respond(payload);
        }
    }
}
