using System.Threading;
using CoAP.EndPoint.Resources;

namespace CoAP.Examples.Resources
{
    /// <summary>
    /// Represents a resource that returns a response in a separate CoAP message.
    /// </summary>
    class SeparateResource : LocalResource
    {
        public SeparateResource()
            : base("separate")
        {
            Title = "GET a response in a separate CoAP Message";
            ResourceType = "SepararateResponseTester";
        }

        public override void DoGet(Request request)
        {
            // Accept the request to promise the client this request will be acted.
            request.Accept();

            // Do sth. time-consuming
            Thread.Sleep(1000);

            // Now respond the previous request.
            Response response = new Response(Code.Content);
            response.PayloadString = "This message was sent by a separate response.\n" +
                "Your client will need to acknowledge it, otherwise it will be retransmitted.";

            request.Respond(response);
        }
    }
}
