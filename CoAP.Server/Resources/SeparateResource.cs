using System;
using System.Threading;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    /// <summary>
    /// Represents a resource that returns a response in a separate CoAP message.
    /// </summary>
    class SeparateResource : Resource
    {
        public SeparateResource(String name)
            : base(name)
        {
            Attributes.Title = "GET a response in a separate CoAP Message";
            Attributes.AddResourceType("SepararateResponseTester");
        }

        protected override void DoGet(CoapExchange exchange)
        {
            // Accept the request to promise the client this request will be acted.
            exchange.Accept();

            // Do sth. time-consuming
            Thread.Sleep(2000);

            // Now respond the previous request.
            Response response = new Response(Code.Content);
            response.PayloadString = "This message was sent by a separate response.\n" +
                "Your client will need to acknowledge it, otherwise it will be retransmitted.";

            exchange.Respond(response);
        }
    }
}
