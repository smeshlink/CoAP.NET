/*
 * Copyright (c) 2011-2012, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using CoAP.Log;

namespace CoAP.EndPoint
{
    public class LocalEndPoint : EndPoint
    {
        private static ILogger log = LogManager.GetLogger(typeof(LocalEndPoint));
        private Resource _root;

        public LocalEndPoint()
        {
            Communicator.ListeningPort = CoapConstants.DefaultPort;
            Communicator.Instance.RegisterReceiver(this);
            _root = new RootResource();
            AddResource(new DiscoveryResource(_root));
        }

        public void AddResource(LocalResource resource)
        {
            _root.AddSubResource(resource);
        }

        public LocalResource GetResource(String path)
        {
            return (LocalResource)_root.GetResource(path);
        }

        private class RootResource : LocalResource
        {
            public RootResource()
                : base(String.Empty, true)
            { }

            public override void DoGet(Request request)
            {
                Response response = new Response(Code.Content);
                response.PayloadString = "Ni Hao from CoAP.NET";
                request.Respond(response);
            }
        }

        protected override void DoHandleMessage(Request request)
        {
            Execute(request);
        }

        protected override void DoHandleMessage(Response response)
        {
            
        }

        protected override void DoExecute(Request request)
        {
            if (request != null)
            {
                String path = request.UriPath;
                LocalResource resource = GetResource(path);

                if (resource != null)
                {
                    request.Resource = resource;
                    if (log.IsDebugEnabled)
                        log.Debug("Dispatching execution: " + path);
                    request.Dispatch(resource);
                }
                else if (request.Code == Code.PUT)
                {
                    // allows creation of non-existing resources through PUT
                    CreateByPut(path, request);
                }
                else
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Cannot find resource: " + path);
                    request.Respond(Code.NotFound);
                }
            }
        }

        private void CreateByPut(String path, Request request)
        {
            // find existing parent up the path
            String parentIdentifier = path;
            String newIdentifier = "";
            Resource parent = null;
            // FIXME cannot find root by ""
            // will end at rootResource ("")
            do
            {
                newIdentifier = path.Substring(parentIdentifier.LastIndexOf('/') + 1);
                parentIdentifier = parentIdentifier.Substring(0, parentIdentifier.LastIndexOf('/'));
            } while ((parent = GetResource(parentIdentifier)) == null);

            // TODO create
        }
    }
}
