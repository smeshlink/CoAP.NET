using System;
using CoAP.EndPoint;

namespace CoAP.Examples
{
    class CoAPClient
    {
        static void Main(String[] args)
        {
            String method = null;
            Uri uri = null;
            String payload = null;
            Boolean loop = false;
            Boolean byEvent = true;

            if (args.Length == 0)
                PrintUsage();

            Int32 index = 0;
            foreach (String arg in args)
            {
                if (arg[0] == '-')
                {
                    if (arg.Equals("-l"))
                        loop = true;
                    if (arg.Equals("-e"))
                        byEvent = true;
                    else
                        Console.WriteLine("Unknown option: " + arg);
                }
                else
                {
                    switch (index)
                    {
                        case 0:
                            method = arg.ToUpper();
                            break;
                        case 1:
                            try
                            {
                                uri = new Uri(arg);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed parsing URI: " + ex.Message);
                                Environment.Exit(1);
                            }
                            break;
                        case 2:
                            payload = arg;
                            break;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg);
                            break;
                    }
                    index++;
                }
            }

            if (method == null || uri == null)
                PrintUsage();

            Request request = CreateRequest(method);
            if (request == null)
            {
                Console.WriteLine("Unknown method: " + method);
                Environment.Exit(1);
            }

            if ("OBSERVE".Equals(method))
            {
                request.SetOption(Option.Create(OptionType.Observe, 0));
                loop = true;
            }

            if ("DISCOVER".Equals(method) && 
                (String.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath.Equals("/")))
            { 
                uri = new Uri(uri, "/.well-known/core");
            }

            request.URI = uri;
            request.SetPayload(payload);
            request.Token = TokenManager.Instance.AcquireToken();
            request.ResponseQueueEnabled = true;
            request.SeparateResponseEnabled = true;

            Console.WriteLine(request);

            try
            {
                if (byEvent)
                {
                    request.Responded += delegate(Object sender, ResponseEventArgs e)
                    {
                        Response response = e.Response;
                        if (response == null)
                        {
                            Console.WriteLine("Request timeout");
                        }
                        else
                        {
                            Console.WriteLine(response);
                            Console.WriteLine("Time (ms): " + response.RTT);
                        }
                        if (!response.IsEmptyACK && !loop)
                            Environment.Exit(0);
                    };
                    request.Execute();
                    while (true)
                    {
                        Console.ReadKey();
                    }
                }
                else
                {
                    request.Execute();

                    do
                    {
                        Console.WriteLine("Receiving response...");

                        Response response = null;
                        response = request.ReceiveResponse();

                        if (response == null)
                        {
                            Console.WriteLine("Request timeout");
                        }
                        else
                        {
                            Console.WriteLine(response);
                            Console.WriteLine("Time (ms): " + response.RTT);

                            if (response.ContentType == MediaType.ApplicationLinkFormat)
                            {
                                Resource root = RemoteResource.NewRoot(response.PayloadString);
                                if (root == null)
                                {
                                    Console.WriteLine("Failed parsing link format");
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    Console.WriteLine("Discovered resources:");
                                    Console.WriteLine(root);
                                }
                            }
                        }
                    } while (loop);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed executing request: " + ex.Message);
                Environment.Exit(1);
            }
        }

        private static Request CreateRequest(String method)
        {
            switch (method)
            {
                case "POST":
                    return new Request(Code.POST);
                case "PUT":
                    return new Request(Code.PUT);
                case "DELETE":
                    return new Request(Code.DELETE);
                case "GET":
                case "DISCOVER":
                case "OBSERVE":
                    return new Request(Code.GET);
                default:
                    return null;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("CoAP.NET Example Client");
		    Console.WriteLine();
            Console.WriteLine("Usage: CoAPClient [-e] [-l] method uri [payload]");
		    Console.WriteLine("  method  : { GET, POST, PUT, DELETE, DISCOVER, OBSERVE }");
            Console.WriteLine("  uri     : The CoAP URI of the remote endpoint or resource.");
            Console.WriteLine("  payload : The data to send with the request.");
            Console.WriteLine("Options:");
            Console.WriteLine("  -e      : Receives responses by the Responded event.");
		    Console.WriteLine("  -l      : Loops for multiple responses.");
		    Console.WriteLine("            (automatic for OBSERVE and separate responses)");
		    Console.WriteLine();
		    Console.WriteLine("Examples:");
            Console.WriteLine("  CoAPClient DISCOVER coap://localhost");
            Console.WriteLine("  CoAPClient POST coap://localhost/storage data");
            Environment.Exit(0);
        }
    }
}
