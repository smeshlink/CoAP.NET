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
using System.IO;
using CoAP.Http;
using CoAP.Util;

namespace CoAP.Layers
{
    /// <summary>
    /// Class encapsulating the logic of a http server.
    /// TODO to be done
    /// </summary>
    public class HttpStack : UpperLayer, IShutdown
    {
        private const String ServerName = "CoAP.NET HTTP Cross-Proxy";

        /// <summary>
        /// Resource associated with the proxying behavior.
        /// If a client requests resource indicated by
        /// http://proxy-address/ProxyResourceName/coap-server, the proxying
        /// handler will forward the request desired coap server.
        /// </summary>
        const String ProxyResourceName = "proxy";
        /// <summary>
        /// The resource associated with the local resources behavior.
        /// If a client requests resource indicated by
        /// http://proxy-address/LocalResourceName/coap-resource, the proxying
        /// handler will forward the request to the local resource requested.
        /// </summary>
        const String LocalResourceName = "local";

        private readonly WebServer _webServer;

        public HttpStack(Int32 httpPort)
        {
            _webServer = new WebServer(ServerName, httpPort);
            _webServer.AddProvider(new BaseRequestHandler());
            _webServer.AddProvider(new ProxyRequestHandler(ProxyResourceName, true));
            _webServer.AddProvider(new ProxyRequestHandler(LocalResourceName, false));
        }

        public void Shutdown()
        {
            _webServer.Stop();
        }

        public Boolean IsWaitingRequest(Request request)
        {
            return false;
        }

        class BaseRequestHandler : CoAP.Http.IServiceProvider
        {
            public Boolean Accept(IHttpRequest request)
            {
                return true;
            }

            public void Process(IHttpRequest request, IHttpResponse response)
            {
                response.StatusCode = 200;
                StreamWriter writer = new StreamWriter(response.OutputStream);
                writer.Write("CoAP.NET Proxy server");
                writer.Flush();
            }
        }

        class ProxyRequestHandler : CoAP.Http.IServiceProvider
        {
            readonly String _uri;
            readonly String _localResource;
            readonly Boolean _proxyingEnabled;

            public ProxyRequestHandler(String localResource, Boolean proxyingEnabled)
            {
                _localResource = localResource;
                _proxyingEnabled = proxyingEnabled;
                _uri = "/" + localResource + "/";
            }

            public Boolean Accept(IHttpRequest request)
            {
                return request.RequestUri.StartsWith(_uri);
            }

            public void Process(IHttpRequest httpRequest, IHttpResponse httpResponse)
            {
                Request coapRequest = HttpTranslator.GetCoapRequest(httpRequest, _localResource, _proxyingEnabled);
            }
        }
    }
}
