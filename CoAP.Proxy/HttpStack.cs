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
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using CoAP.Http;
using CoAP.Threading;
using CoAP.Util;

namespace CoAP.Proxy
{
    /// <summary>
    /// Class encapsulating the logic of a http server. The class create a receiver
    /// thread that it is always blocked on the listen primitive. For each connection
    /// this thread creates a new thread that handles the client/server dialog.
    /// </summary>
    public class HttpStack
    {
        private const String ServerName = "CoAP.NET HTTP Proxy";

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

        private static readonly Int32 GatewayTimeout = 100000 * 3 / 4;
        readonly WebServer _webServer;
        readonly ConcurrentDictionary<Request, WaitFuture<Request, Response>> _exchangeMap = new ConcurrentDictionary<Request, WaitFuture<Request, Response>>();
        private IExecutor _executor = Executors.Default;

        public Action<Request> RequestHandler;

        public HttpStack(Int32 httpPort)
        {
            _webServer = new WebServer(ServerName, httpPort);
            _webServer.AddProvider(new BaseRequestHandler());
            _webServer.AddProvider(new ProxyRequestHandler(this, ProxyResourceName, true));
            _webServer.AddProvider(new ProxyRequestHandler(this, LocalResourceName, false));
        }

        public void Stop()
        {
            _webServer.Stop();
        }

        public Boolean IsWaitingRequest(Request request)
        {
            return _exchangeMap.ContainsKey(request);
        }

        public void DoSendResponse(Request request, Response response)
        {
            // the http stack is intended to send back only coap responses

            // fill the exchanger with the incoming response
            WaitFuture<Request, Response> wf;
            if (_exchangeMap.TryRemove(request, out wf))
            {
                wf.Response = response;
            }
        }

        public void DoReceiveMessage(Request request)
        {
            Action<Request> handler = RequestHandler;
            if (handler != null)
                handler(request);
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
                writer.Write(ServerName);
                writer.Flush();
            }
        }

        class ProxyRequestHandler : CoAP.Http.IServiceProvider
        {
            readonly HttpStack _httpStack;
            readonly String _uri;
            readonly String _localResource;
            readonly Boolean _proxyingEnabled;

            public ProxyRequestHandler(HttpStack httpStack, String localResource, Boolean proxyingEnabled)
            {
                _httpStack = httpStack;
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
                try
                {
                    Request coapRequest = HttpTranslator.GetCoapRequest(httpRequest, _localResource, _proxyingEnabled);

                    WaitFuture<Request, Response> wf = new WaitFuture<Request, Response>(coapRequest);
                    _httpStack._exchangeMap[coapRequest] = wf;

                    // send the coap request to the upper layers
                    _httpStack._executor.Start(() => _httpStack.DoReceiveMessage(coapRequest));

                    Response coapResponse;
                    try
                    {
                        wf.Wait(GatewayTimeout);
                        coapResponse = wf.Response;
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                        httpResponse.StatusCode = (Int32)HttpStatusCode.InternalServerError;
                        return;
                    }

                    if (coapResponse == null)
                    {
                        httpResponse.StatusCode = (Int32)HttpStatusCode.GatewayTimeout;
                    }
                    else
                    {
                        HttpTranslator.GetHttpResponse(httpRequest, coapResponse, httpResponse);
                    }
                }
                catch (TranslationException)
                {
                    httpResponse.StatusCode = (Int32)HttpStatusCode.BadGateway;
                }
            }
        }
    }
}
