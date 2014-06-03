using System;
using System.IO;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    class ImageResource : Resource
    {
        private Int32[] _supported = new Int32[] {
            MediaType.ImageJpeg,
            MediaType.ImagePng
        };

        public ImageResource(String name)
            : base(name)
        {
            Attributes.Title = "GET an image with different content-types";
            Attributes.AddResourceType("Image");

            foreach (Int32 item in _supported)
            {
                Attributes.AddContentType(item);
            }

            Attributes.MaximumSizeEstimate = 18029;
        }

        protected override void DoGet(CoapExchange exchange)
        {
            String file = "data\\image\\";
            Int32 ct = MediaType.ImagePng;
            Request request = exchange.Request;

            if ((ct = MediaType.NegotiationContent(ct, _supported, request.GetOptions(OptionType.Accept)))
                == MediaType.Undefined)
            {
                exchange.Respond(Code.NotAcceptable);
            }
            else
            {
                file += "image." + MediaType.ToFileExtension(ct);
                if (File.Exists(file))
                {
                    Byte[] data = null;
                    
                    try
                    {
                        data = File.ReadAllBytes(file);
                    }
                    catch (Exception ex)
                    {
                        exchange.Respond(Code.InternalServerError, "IO error");
                        Console.WriteLine(ex.Message);
                    }

                    Response response = new Response(Code.Content);
                    response.Payload = data;
                    response.ContentType = ct;
                    exchange.Respond(response);
                }
                else
                {
                    exchange.Respond(Code.InternalServerError, "Image file not found");
                }
            }
        }
    }
}
