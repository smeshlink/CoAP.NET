using CoAP.EndPoint.Resources;

namespace CoAP.Examples.Resources
{
    class HelloWorldResource : LocalResource
    {
        public HelloWorldResource()
            : base("helloWorld")
        {
            Title = "GET a friendly greeting!";
            ResourceType = "HelloWorldDisplayer";
        }

        public override void DoGet(Request request)
        {
            Response response = new Response(Code.Content);
            response.PayloadString = "Hello World!";
            request.Respond(response);
        }
    }
}
