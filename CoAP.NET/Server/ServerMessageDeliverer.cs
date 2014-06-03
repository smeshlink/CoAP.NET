﻿/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using CoAP.Log;
using CoAP.Net;
using CoAP.Observe;
using CoAP.Server.Resources;
using CoAP.Util;

namespace CoAP.Server
{
    /// <summary>
    /// Delivers requests to corresponding resources and
    /// responses to corresponding requests.
    /// </summary>
    public class ServerMessageDeliverer : IMessageDeliverer
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(ServerMessageDeliverer));
        readonly IResource _root;
        readonly ObserveManager _observeManager = new ObserveManager();

        /// <summary>
        /// Constructs a default message deliverer that delivers requests
        /// to the resources rooted at the specified root.
        /// </summary>
        public ServerMessageDeliverer(IResource root)
        {
            _root = root;
        }

        /// <inheritdoc/>
        public void DeliverRequest(Exchange exchange)
        {
            Request request = exchange.Request;
            IResource resource = FindResource(request.UriPaths);
            if (resource != null)
            {
                CheckForObserveOption(exchange, resource);

                // TODO threading
                resource.HandleRequest(exchange);
            }
            else
            {
                exchange.SendResponse(new Response(Code.NotFound));
            }
        }

        /// <inheritdoc/>
        public void DeliverResponse(Exchange exchange, Response response)
        {
            if (exchange == null)
                ThrowHelper.ArgumentNullException("exchange");
            if (response == null)
                ThrowHelper.ArgumentNullException("response");
            if (exchange.Request == null)
                throw new ArgumentException("Request should not be empty.", "exchange");
            exchange.Request.Response = response;
        }

        private IResource FindResource(IEnumerable<String> paths)
        {
            IResource current = _root;
            using (IEnumerator<String> ie = paths.GetEnumerator())
            {
                while (ie.MoveNext() && current != null)
                {
                    current = current.GetChild(ie.Current);
                }
            }
            return current;
        }

        private void CheckForObserveOption(Exchange exchange, IResource resource)
        {
            Request request = exchange.Request;
            if (request.Code != Code.GET)
                return;

            System.Net.EndPoint source = request.Source;
            if (request.HasOption(OptionType.Observe) && resource.Observable)
            {
                Int32 obs = request.Observe;
                if (obs == 0)
                {
                    // Requests wants to observe and resource allows it :-)
                    if (log.IsDebugEnabled)
                        log.Debug("Initiate an observe relation between " + source + " and resource " + resource.Uri);
                    ObservingEndpoint remote = _observeManager.FindObservingEndpoint(source);
                    ObserveRelation relation = new ObserveRelation(remote, resource, exchange);
                    remote.AddObserveRelation(relation);
                    exchange.Relation = relation;
                    // all that's left is to add the relation to the resource which
                    // the resource must do itself if the response is successful
                }
                else if (obs == 1)
                {
                    ObserveRelation relation = _observeManager.GetRelation(source, request.Token);
                    if (relation != null)
                        relation.Cancel();
                }
            }
        }
    }
}