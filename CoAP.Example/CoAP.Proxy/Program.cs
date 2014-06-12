using System;
using CoAP.Proxy;
using CoAP.Proxy.Resources;
using CoAP.Server;
using CoAP.Server.Resources;

namespace CoAP.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            ForwardingResource coap2coap = new ProxyCoapClientResource("coap2coap");
            ForwardingResource coap2http = new ProxyHttpClientResource("coap2http");

            // Create CoAP Server on PORT with proxy resources form CoAP to CoAP and HTTP
            CoapServer coapServer = new CoapServer(CoapConfig.Default.DefaultPort);
            coapServer.Add(coap2coap);
            coapServer.Add(coap2http);
            coapServer.Add(new TargetResource("target"));
            coapServer.Start();

            ProxyHttpServer httpServer = new ProxyHttpServer(CoapConfig.Default.HttpPort);
            httpServer.ProxyCoapResolver = new DirectProxyCoAPResolver(coap2coap);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        class TargetResource : Resource
        {
            private Int32 _counter;

            public TargetResource(String name)
                : base(name)
            { }

            protected override void DoGet(CoapExchange exchange)
            {
                exchange.Respond("Response " + (++_counter) + " from resource " + Name);
            }
        }
    }
}
