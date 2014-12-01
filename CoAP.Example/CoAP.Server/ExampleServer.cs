using System;
using CoAP.Examples.Resources;
using CoAP.Server;

namespace CoAP.Examples
{
    public class ExampleServer
    {
        public static void Main(String[] args)
        {
            CoapServer server = new CoapServer();

            server.Add(new HelloWorldResource("hello"));
            server.Add(new FibonacciResource("fibonacci"));
            server.Add(new StorageResource("storage"));
            server.Add(new ImageResource("image"));
            server.Add(new MirrorResource("mirror"));
            server.Add(new LargeResource("large"));
            server.Add(new CarelessResource("careless"));
            server.Add(new SeparateResource("separate"));
            server.Add(new TimeResource("time"));

            try
            {
                server.Start();

                Console.Write("CoAP server [{0}] is listening on", server.Config.Version);

                foreach (var item in server.EndPoints)
                {
                    Console.Write(" ");
                    Console.Write(item.LocalEndPoint);
                }
                Console.WriteLine();
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
