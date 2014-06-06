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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Messaging;

namespace CoAP.Http
{
    class WebServer
    {
        private readonly String _name;
        private readonly List<IServiceProvider> _serviceProviders = new List<IServiceProvider>();
        private readonly IChannel _channel;

        public WebServer(String name, Int32 port)
        {
            _name = name;
            _channel = new HttpServerChannel(name, port, new WebServerFormatterSinkProvider(this));
        }

        public void AddProvider(IServiceProvider provider)
        {
            _serviceProviders.Add(provider);
        }

        public void Start()
        {
            ChannelServices.RegisterChannel(_channel, false);
        }

        public void Stop()
        {
            try
            {
                ChannelServices.UnregisterChannel(_channel);
            }
            catch (Exception) { }
        }

        class WebServerFormatterSinkProvider : IServerFormatterSinkProvider
        {
            private readonly WebServer _webServer;
            private IServerChannelSinkProvider _next;

            public WebServerFormatterSinkProvider(WebServer webServer)
            {
                _webServer = webServer;
            }

            public IServerChannelSinkProvider Next
            {
                get { return _next; }
                set { _next = value; }
            }

            public IServerChannelSink CreateSink(IChannelReceiver channel)
            {
                IServerChannelSink sink = null;
                if (Next != null)
                    sink = Next.CreateSink(channel);
                return new WebServerChannelSink(sink, channel, _webServer);
            }

            public void GetChannelData(IChannelDataStore channelData)
            { }
        }

        class WebServerChannelSink : IServerChannelSink
        {
            private readonly WebServer _webServer;
            private readonly IServerChannelSink _nextChannelSink;

            public WebServerChannelSink(IServerChannelSink next, IChannelReceiver channel, WebServer webServer)
            {
                _webServer = webServer;
                _nextChannelSink = next;
            }

            public IServerChannelSink NextChannelSink
            {
                get { return _nextChannelSink; }
            }

            public IDictionary Properties
            {
                get { throw new NotImplementedException(); }
            }

            public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, Object state, IMessage msg, ITransportHeaders headers, Stream stream)
            {
                throw new NotImplementedException();
            }

            public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, Object state, IMessage msg, ITransportHeaders headers)
            {
                throw new NotImplementedException();
            }

            public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg, out ITransportHeaders responseHeaders, out Stream responseStream)
            {
                if (requestMsg != null)
                {
                    return NextChannelSink.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
                        out responseMsg, out responseHeaders, out responseStream);
                }

                IHttpRequest request = GetRequest(requestHeaders, requestStream);
                IHttpResponse response = GetResponse(request);

                foreach (IServiceProvider provider in _webServer._serviceProviders)
                {
                    if (provider.Accept(request))
                    {
                        provider.Process(request, response);
                        break;
                    }
                }

                response.AppendHeader("Server", _webServer._name);

                responseHeaders = (response as RemotingHttpResponse).Headers;
                responseStream = response.OutputStream;
                responseMsg = null;

                return ServerProcessing.Complete;
            }

            private IHttpResponse GetResponse(IHttpRequest request)
            {
                RemotingHttpResponse response = new RemotingHttpResponse();
                return response;
            }

            private IHttpRequest GetRequest(ITransportHeaders requestHeaders, Stream requestStream)
            {
                return new RemotingHttpRequest(requestHeaders, requestStream);
            }
        }
    }
}
