/*
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

namespace CoAP.Server.Resources
{
    /// <summary>
    /// Represents the CoAP .well-known/core resource.
    /// </summary>
    public class DiscoveryResource : Resource
    {
        public static readonly String Core = "core";
        readonly Resource _root;

        /// <summary>
        /// Instantiates a new discovery resource.
        /// </summary>
        public DiscoveryResource(Resource root)
            : this(Core, root)
        { }

        /// <summary>
        /// Instantiates a new discovery resource with the specified name.
        /// </summary>
        public DiscoveryResource(String name, Resource root)
            : base(name)
        {
            _root = root;
        }

        /// <inheritdoc/>
        protected override void HandleGET(CoapExchange exchange)
        {
            exchange.Respond(Code.Content,
                LinkFormat.Serialize(_root, exchange.Request.GetOptions(OptionType.UriQuery)),
                MediaType.ApplicationLinkFormat);
        }
    }
}
