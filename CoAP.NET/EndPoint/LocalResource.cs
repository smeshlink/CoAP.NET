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

namespace CoAP.EndPoint
{
    public class LocalResource : Resource
    {
        public LocalResource(String resourceIdentifier)
            : base(resourceIdentifier)
        { }

        /// <summary>
        /// Creates a resouce instance with proper subtype.
        /// </summary>
        /// <returns></returns>
        protected override Resource CreateInstance(String name)
        {
            return new LocalResource(name);
        }
    }
}
