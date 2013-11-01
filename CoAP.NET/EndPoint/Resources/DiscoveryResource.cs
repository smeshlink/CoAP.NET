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
using System.Collections.Generic;

namespace CoAP.EndPoint.Resources
{
    /// <summary>
    /// Represents the CoAP .well-known/core resource.
    /// </summary>
    public class DiscoveryResource : LocalResource
    {
        public static readonly String DefaultIdentifier = ".well-known/core";
        private Resource _root;

        public DiscoveryResource(Resource root)
            : base(DefaultIdentifier, true)
        {
            ContentTypeCode = MediaType.ApplicationLinkFormat;
            _root = root;
        }

        public override void DoGet(Request request)
        {
            Response response = new Response(Code.Content);
            IEnumerable<Option> query = request.GetOptions(OptionType.UriQuery);
            response.SetPayload(LinkFormat.Serialize(_root, query, true), MediaType.ApplicationLinkFormat);
            request.Respond(response);
        }
    }
}
