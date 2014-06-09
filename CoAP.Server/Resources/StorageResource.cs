using System;
using System.Collections.Generic;
using System.Text;
using CoAP.Server.Resources;

namespace CoAP.Examples.Resources
{
    class StorageResource : Resource
    {
        private String _content;

        public StorageResource(String name)
            : base(name)
        { }

        protected override void DoGet(CoapExchange exchange)
        {
            if (_content != null)
            {
                exchange.Respond(_content);
            }
            else
            {
                String subtree = LinkFormat.Serialize(this, null);
                exchange.Respond(StatusCode.Content, subtree, MediaType.ApplicationLinkFormat);
            }
        }

        protected override void DoPost(CoapExchange exchange)
        {
            String payload = exchange.Request.PayloadString;
            if (payload == null)
                payload = String.Empty;
            String[] parts = payload.Split('\\');
            String[] path = parts[0].Split('/');
            IResource resource = Create(new LinkedList<String>(path));

            Response response = new Response(StatusCode.Created);
            response.LocationPath = resource.Uri;
            exchange.Respond(response);
        }

        protected override void DoPut(CoapExchange exchange)
        {
            _content = exchange.Request.PayloadString;
            exchange.Respond(StatusCode.Changed);
        }

        protected override void DoDelete(CoapExchange exchange)
        {
            this.Delete();
            exchange.Respond(StatusCode.Deleted);
        }

        private IResource Create(LinkedList<String> path)
        {
            String segment;

            do
            {
                if (path.Count == 0)
                    return this;
                segment = path.First.Value;
                path.RemoveFirst();
            } while (segment.Length == 0 || segment.Equals("/"));

            StorageResource resource = new StorageResource(segment);
            Add(resource);
            return resource.Create(path);
        }
    }
}
