using System;
using System.Collections.Generic;
using System.Text;
using CoAP.EndPoint;

namespace CoAP.Examples
{
    class Program
    {
#if COAPALL
        static ISpec Spec = CoAP.Spec.Draft13;
#endif

        static void Main(string[] args)
        {
            try
            {
                ProxyEndpoint proxy = new ProxyEndpoint();
                Console.WriteLine("CoAP server [{1}] is listening on port {0}.",
                    proxy.Communicator.Port,
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
