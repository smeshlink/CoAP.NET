using CoAP.EndPoint;

namespace CoAP.Examples.Resources
{
    /// <summary>
    /// Represents a resource that forgets to return a separate response.
    /// </summary>
    class CarelessResource : LocalResource
    {
        public CarelessResource()
            : base("careless")
        {
            Title = "This resource will ACK anything, but never send a separate response";
            ResourceType = "SepararateResponseTester";
        }

        public override void DoGet(Request request)
        {
            // Accept the request to promise the client this request will be acted.
            request.Accept();
            
            // ... and then do nothing. Pretty mean...
        }
    }
}
