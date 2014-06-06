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

#if COAPALL
        private static IEndPoint _draft03;
        private static IEndPoint _draft08;
        private static IEndPoint _draft12;
        private static IEndPoint _draft13;

        public static IEndPoint Draft03
        {
            get
            {
                if (_draft03 == null)
                {
                    lock (typeof(EndPointManager))
                    {
                        if (_draft03 == null)
                            _draft03 = CreateEndPoint(Spec.Draft03);
                    }
                }
                return _draft03;
            }
        }

        public static IEndPoint Draft08
        {
            get
            {
                if (_draft08 == null)
                {
                    lock (typeof(EndPointManager))
                    {
                        if (_draft08 == null)
                            _draft08 = CreateEndPoint(Spec.Draft08);
                    }
                }
                return _draft08;
            }
        }

        public static IEndPoint Draft12
        {
            get
            {
                if (_draft12 == null)
                {
                    lock (typeof(EndPointManager))
                    {
                        if (_draft12 == null)
                            _draft12 = CreateEndPoint(Spec.Draft12);
                    }
                }
                return _draft12;
            }
        }

        public static IEndPoint Draft13
        {
            get
            {
                if (_draft13 == null)
                {
                    lock (typeof(EndPointManager))
                    {
                        if (_draft13 == null)
                            _draft13 = CreateEndPoint(Spec.Draft13);
                    }
                }
                return _draft13;
            }
        }

        public static IEndPoint Draft18
        {
            get { return Default; }
        }
#endif

        private static IEndPoint CreateEndPoint()
        {
            CoAPEndPoint ep = new CoAPEndPoint(0);
            ep.Start();
            return ep;
        }

#if COAPALL
        public static IEndPoint CreateEndPoint(ISpec spec)
        {
            CoapConfig config = new CoapConfig();
            config.DefaultPort = spec.DefaultPort;
            config.Spec = spec;
            CoAPEndPoint ep = new CoAPEndPoint(config);
            ep.Start();
            return ep;
        }
#endif
    }
}
