using System;
using CoAP.EndPoint;
using CoAP.Examples.Resources;

namespace CoAP.Examples
{
    class CoAPServer : LocalEndPoint
    {
        public CoAPServer()
        {
            AddResource(new HelloWorldResource());
            AddResource(new CarelessResource());
            AddResource(new ImageResource());
            AddResource(new SeparateResource());
            AddResource(new TimeResource());
        }

        static void Main(String[] args)
        {
            CoAPServer server = new CoAPServer();
            Console.WriteLine("CoAP server is listening on port {0}. Press any key to exit.", Communicator.Instance.Port);
            Console.ReadKey();
        }
    }
}
