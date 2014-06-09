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
using CoAP.Log;
using CoAP.Net;

namespace CoAP.Proxy
{
    public class ProxyHttpServer
    {
        static readonly ILogger log = LogManager.GetLogger(typeof(ProxyHttpServer));
        static readonly String ProxyCoapClient = "proxy/coapClient";
        static readonly String ProxyHttpClient = "proxy/httpClient";
        readonly ICacheResource _cacheResource = new NoCache(); // TODO cache implementation
        private IProxyCoAPResolver _proxyCoapResolver;
        private HttpStack _httpStack;
        private ProxyEndPoint _proxyEndPoint;

        public ProxyHttpServer()
            : this(CoapConfig.Default.HttpPort)
        { }

        public ProxyHttpServer(Int32 httpPort)
        {
            _httpStack = new HttpStack(httpPort);
            _httpStack.RequestHandler = HandleRequest;
            _proxyEndPoint = new ProxyEndPoint(this);
        }

        public IProxyCoAPResolver ProxyCoapResolver
        {
            get { return _proxyCoapResolver; }
            set { _proxyCoapResolver = value; }
        }

        private void HandleRequest(Request request)
        {
            Exchange exchange = new Exchange(request, Origin.Remote);
            exchange.EndPoint = _proxyEndPoint;

            Response response = null;
            // ignore the request if it is reset or acknowledge
            // check if the proxy-uri is defined
            if (request.Type != MessageType.RST && request.Type != MessageType.ACK
                    && request.HasOption(OptionType.ProxyUri))
            {
                // get the response from the cache
                response = _cacheResource.GetResponse(request);

                // TODO update statistics
                //_statsResource.updateStatistics(request, response != null);
            }

            // check if the response is present in the cache
            if (response != null)
            {
                // link the retrieved response with the request to set the
                // parameters request-specific (i.e., token, id, etc)
                exchange.SendResponse(response);
                return;
            }
            else
            {

                // edit the request to be correctly forwarded if the proxy-uri is
                // set
                if (request.HasOption(OptionType.ProxyUri))
                {
                    try
                    {
                        ManageProxyUriRequest(request);
                    }
                    catch (Exception)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("Proxy-uri malformed: " + request.GetFirstOption(OptionType.ProxyUri).StringValue);

                        exchange.SendResponse(new Response(StatusCode.BadOption));
                    }
                }

                // handle the request as usual
                if (_proxyCoapResolver != null)
                    _proxyCoapResolver.ForwardRequest(exchange);
            }
        }

        protected void ResponseProduced(Request request, Response response)
        {
            // check if the proxy-uri is defined
            if (request.HasOption(OptionType.ProxyUri))
            {
                // insert the response in the cache
                _cacheResource.CacheResponse(request, response);
            }
        }

        private void ManageProxyUriRequest(Request request)
        {
            // check which schema is requested
            Uri proxyUri = request.ProxyUri;

            // the local resource that will abstract the client part of the
            // proxy
            String clientPath;

            // switch between the schema requested
            if (proxyUri.Scheme != null && proxyUri.Scheme.StartsWith("http"))
            {
                // the local resource related to the http client
                clientPath = ProxyHttpClient;
            }
            else
            {
                // the local resource related to the http client
                clientPath = ProxyCoapClient;
            }

            // set the path in the request to be forwarded correctly
            request.UriPath = clientPath;
        }

        class ProxyEndPoint : IEndPoint
        {
            readonly ProxyHttpServer _server;

            public ProxyEndPoint(ProxyHttpServer server)
            {
                _server = server;
            }

            public void SendResponse(Exchange exchange, Response response)
            {
                // Redirect the response to the HttpStack instead of a normal
                // CoAP endpoint.
                exchange.Request.Response = response;
                try
                {
                    _server.ResponseProduced(exchange.Request, response);
                    _server._httpStack.DoSendResponse(exchange.Request, response);
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Exception while responding to Http request", e);
                }
            }

            public ICoapConfig Config
            {
                get { throw new NotImplementedException(); }
            }

            public System.Net.EndPoint LocalEndPoint
            {
                get { throw new NotImplementedException(); }
            }

            public bool Running
            {
                get { throw new NotImplementedException(); }
            }

            public IMessageDeliverer MessageDeliverer
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public Stack.IExchangeForwarder ExchangeForwarder
            {
                get { throw new NotImplementedException(); }
            }

            public void Start()
            {
                throw new NotImplementedException();
            }

            public void Stop()
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public void SendRequest(Request request)
            {
                throw new NotImplementedException();
            }

            public void SendEmptyMessage(Exchange exchange, EmptyMessage message)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        class NoCache : ICacheResource
        {
            public void CacheResponse(Request request, Response response)
            { }

            public Response GetResponse(Request request)
            {
                return null;
            }

            public void InvalidateRequest(Request request)
            { }
        }
    }
}
