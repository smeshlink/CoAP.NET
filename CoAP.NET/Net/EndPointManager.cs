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

namespace CoAP.Net
{
    public static class EndPointManager
    {
        private static IEndPoint _default;

        public static IEndPoint Default
        {
            get
            {
                if (_default == null)
                {
                    lock (typeof(EndPointManager))
                    {
                        if (_default == null)
                        {
                            _default = CreateEndPoint();
                        }
                    }
                }
                return _default;
            }
        }

        private static IEndPoint CreateEndPoint()
        {
            CoAPEndPoint ep = new CoAPEndPoint(0);
            ep.Start();
            return ep;
        }
    }
}
