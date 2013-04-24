using System;
using System.IO;
using CoAP.EndPoint;

namespace CoAP.Examples.Resources
{
    class ImageResource : LocalResource
    {
        private Int32[] _supported = new Int32[] {
            MediaType.ImageJpeg
        };

        public ImageResource()
            : base("image")
        {
            Title = "GET an image with different content-types";
            ResourceType = "Image";

            foreach (var item in _supported)
            {
                ContentTypeCode = item;
            }

            MaximumSizeEstimate = 18029;
        }

        public override void DoGet(Request request)
        {
            String file = "data\\image\\";
            Int32 ct = MediaType.ImagePng;

            if ((ct = MediaType.NegotiationContent(ct, _supported, request.GetOptions(OptionType.Accept)))
                == MediaType.Undefined)
            {
                request.Respond(Code.NotAcceptable, "Accept only gif, jpeg or png");
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
                        request.Respond(Code.InternalServerError, "IO error");
                        Console.WriteLine(ex.Message);
                    }

                    Response response = new Response(Code.Content);
                    response.Payload = data;
                    response.ContentType = ct;
                    request.Respond(response);
                }
                else
                {
                    request.Respond(Code.InternalServerError, "Representation not found");
                }
            }
        }
    }
}
