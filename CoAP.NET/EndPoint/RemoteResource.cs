/*
 * Copyright (c) 2011, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;

namespace CoAP.EndPoint
{
    public class RemoteResource : Resource
    {
        public static RemoteResource NewRoot(String linkFormat)
        {
            RemoteResource resource = new RemoteResource();
            resource.Type = "root";
            resource.Title = "Root";
            resource.AddLinkFormat(linkFormat);
            return resource;
        }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected override Resource CreateInstance()
        {
            return new RemoteResource();
        }
    }
}
