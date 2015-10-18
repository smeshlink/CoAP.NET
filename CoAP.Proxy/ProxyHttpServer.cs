/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
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

        public ProxyHttpServer()
            : this(CoapConfig.Default.HttpPort)
        { }

        public ProxyHttpServer(Int32 httpPort)
        {
            _httpStack = new HttpStack(httpPort);
            _httpStack.RequestHandler = HandleRequest;
        }

        public IProxyCoAPResolver ProxyCoapResolver
        {
            get { return _proxyCoapResolver; }
            set { _proxyCoapResolver = value; }
        }

        private void HandleRequest(Request request)
        {
            Exchange exchange = new ProxyExchange(this, request);
            exchange.Request = request;

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

        class ProxyExchange : Exchange
        {
            readonly ProxyHttpServer _server;
            readonly Request _request;

            public ProxyExchange(ProxyHttpServer server, Request request)
                : base(request, Origin.Remote)
            {
                _server = server;
                _request = request;
            }

            public override void SendAccept()
            {
                // has no meaning for HTTP: do nothing
            }

            public override void SendReject()
            {
                // TODO: close the HTTP connection to signal rejection
            }

            public override void SendResponse(Response response)
            {
                // Redirect the response to the HttpStack instead of a normal
                // CoAP endpoint.
                // TODO: When we change endpoint to be an interface, we can
                // redirect the responses a little more elegantly.
                try
                {
                    _request.Response = response;
                    _server.ResponseProduced(_request, response);
                    _server._httpStack.DoSendResponse(_request, response);
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("Exception while responding to Http request", e);
                }
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
