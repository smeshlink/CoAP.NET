/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using CoAP.EndPoint.Resources;
using CoAP.Log;

namespace CoAP.EndPoint
{
    /// <summary>
    /// Provides the functionality of a server endpoint.
    /// A server implementation using CoAP.NET will override
    /// this class to provide custom resources. Internally, the main purpose of this
    /// class is to forward received requests to the corresponding resource specified
    /// by the Uri-Path option.
    /// </summary>
    public class LocalEndPoint : EndPoint
    {
        private static readonly ILogger log = LogManager.GetLogger(typeof(LocalEndPoint));

        private Resource _root;
        private Communicator.CommonCommunicator _communicator;

#if COAPALL
        public LocalEndPoint(ISpec spec)
            : this(CoAP.Communicator.CreateCommunicator(spec.DefaultPort, spec.DefaultBlockSize, spec))
        { }
#endif

        public LocalEndPoint()
            : this(Spec.DefaultPort, 0)
        { }

        public LocalEndPoint(Int32 port)
            : this(port, 0)
        { }

        public LocalEndPoint(Int32 port, Int32 transferBlockSize)
            : this(CoAP.Communicator.CreateCommunicator(port, transferBlockSize))
        { }

        public LocalEndPoint(Communicator.CommonCommunicator communicator)
        {
            _communicator = communicator;
            _communicator.RegisterReceiver(this);
            _root = new RootResource();
            AddResource(new DiscoveryResource(_root));
        }

        public Communicator.CommonCommunicator Communicator
        {
            get { return _communicator; }
        }

        /// <summary>
        /// Adds a resource to the root resource of the endpoint. If the resource
        /// identifier is actually a path, it is split up into multiple resources.
        /// </summary>
        /// <param name="resource">the resource to add</param>
        public void AddResource(LocalResource resource)
        {
            _root.AddSubResource(resource);
        }

        /// <summary>
        /// Gets a resource with the given path.
        /// </summary>
        /// <param name="path">the path of the resource</param>
        public LocalResource GetResource(String path)
        {
            return (LocalResource)_root.GetResource(path);
        }

        /// <summary>
        /// Removes a resource with the given path.
        /// </summary>
        /// <param name="path">the path of the resource to remove</param>
        public void RemoveResource(String path)
        {
            _root.RemoveSubResource(path);
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
                    // TODO threading
                    try
                    {
                        request.Dispatch(resource);
                    }
                    catch (Exception ex)
                    {
                        if (log.IsErrorEnabled)
                            log.Error(String.Format("Resource handler {0} crashed: {1}", resource.Name, ex.Message));
                        request.Respond(Code.InternalServerError);
                        request.SendResponse();
                        return;
                    }

                    request.SendResponse();
                }
                else if (request.Code == Code.PUT)
                {
                    // allows creation of non-existing resources through PUT
                    CreateByPut(path, request);
                    request.SendResponse();
                }
                else
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Cannot find resource: " + path);
                    request.Respond(Code.NotFound);
                    request.SendResponse();
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

            parent.CreateSubResource(request, newIdentifier);
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
    }
}
