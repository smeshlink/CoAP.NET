using System;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    /// <summary>
    /// Represents a resource that forgets to return a separate response.
    /// </summary>
    class CarelessResource : Resource
    {
        public CarelessResource(String name)
            : base(name)
        {
            Attributes.Title = "This resource will ACK anything, but never send a separate response";
            Attributes.AddResourceType("SepararateResponseTester");
        }

        protected override void DoGet(CoapExchange exchange)
        {
            // Accept the request to promise the client this request will be acted.
            exchange.Accept();
            
            // ... and then do nothing. Pretty mean...
        }
    }
}
