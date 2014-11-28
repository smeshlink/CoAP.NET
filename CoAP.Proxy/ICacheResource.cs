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

namespace CoAP.Proxy
{
    public interface ICacheResource
    {
        void CacheResponse(Request request, Response response);
        Response GetResponse(Request request);
        void InvalidateRequest(Request request);
    }
}
