using System;
using System.Collections.Generic;
using System.Text;
using CoAP;

namespace CoAP.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri uri = new Uri("coap://[::1]:5683/image?size=2");
            String payload = "";
            Boolean loop = true;

            Request request = Request.Create(Request.Method.GET);
            request.URI = uri;
            request.SetPayload(payload);
            request.SetOption(Option.Create(OptionType.Observe, 60));
            request.SetOption(Option.Create(OptionType.Token, 0xCAFE));
            request.Responded += new EventHandler<ResponseEventArgs>(request_OnResponse);
            request.Responding += new EventHandler<ResponseEventArgs>(request_Responding);
            request.ResponseQueueEnabled = true;
            request.Execute();
            //do
            //{
            //    Console.WriteLine("Receiving response...");

            //    Response response = null;
            //    response = request.ReceiveResponse();

            //    if (response != null && response.IsEmptyACK)
            //    {
            //        Console.WriteLine(response.ToString());
            //        Console.WriteLine("Request acknowledged, waiting for separate response...");
            //        response = request.ReceiveResponse();
            //    }

            //    if (null != response)
            //    {
            //        Console.WriteLine(response.ToString());
            //        Console.WriteLine("Round Trip Time (ms): " + response.RTT);
            //    }
            //    else
            //    {
            //        // no response received
            //        // calculate time elapsed
            //        long elapsed = DateTime.Now.Ticks - request.Timestamp;
            //        TimeSpan span = new TimeSpan(elapsed);
            //        Console.WriteLine("Request timed out (ms): " + span.TotalMilliseconds);
            //        break;
            //    }
            //} while (loop);

            Console.ReadLine();
        }

        static void request_Responding(object sender, ResponseEventArgs e)
        {
            Console.WriteLine(e.Response.PayloadSize);
        }

        static void request_OnResponse(object sender, ResponseEventArgs e)
        {
            Console.WriteLine(DateTime.Now);
            //Console.WriteLine(e.Response.ToString());
        }
    }
}
