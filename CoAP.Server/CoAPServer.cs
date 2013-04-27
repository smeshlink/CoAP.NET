using System;
using CoAP.EndPoint;
using CoAP.Examples.Resources;

namespace CoAP.Examples
{
    class CoAPServer : LocalEndPoint
    {
#if COAPALL
        static ISpec Spec = CoAP.Spec.Draft12;
#endif

        public CoAPServer()
#if COAPALL
            : base(Spec)
#endif
        {
            AddResource(new HelloWorldResource());
            AddResource(new CarelessResource());
            AddResource(new ImageResource());
            AddResource(new SeparateResource());
            AddResource(new TimeResource());
        }

        static void Main(String[] args)
        {
            try
            {
                CoAPServer server = new CoAPServer();
                Console.WriteLine("CoAP server [{1}] is listening on port {0}.",
                    server.Communicator.Port,
                    Spec.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
