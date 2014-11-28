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
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Text;

namespace CoAP.Http
{
    class RemotingHttpRequest : IHttpRequest
    {
        private IDictionary<String, Object> _parameters;
        private IDictionary<Object, Object> _data;
        private NameValueCollection _headers = new NameValueCollection();

        public RemotingHttpRequest(ITransportHeaders headers, Stream stream)
        {
            Method = (String)headers["__RequestVerb"];
            Host = (String)headers["Host"];
            UserAgent = (String)headers["User-Agent"];

            String requestUri = (String)headers["__RequestUri"];
            Url = "http://" + Host + requestUri;

            Int32 offset = requestUri.IndexOf('?');
            if (offset >= 0)
            {
                RequestUri = requestUri.Substring(0, offset);
                QueryString = requestUri.Substring(offset + 1);
            }
            else
            {
                RequestUri = requestUri;
                QueryString = null;
            }

            foreach (DictionaryEntry item in headers)
            {
                if (item.Value is String)
                    _headers.Add((String)item.Key, (String)item.Value);
            }

            InputStream = stream;
        }

        public String Url { get; set; }

        public String RequestUri { get; set; }

        public String QueryString { get; set; }

        public String Method { get; set; }

        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        public Stream InputStream { get; set; }

        public String Host { get; set; }

        public String UserAgent { get; set; }

        public String CharacterEncoding
        {
            get
            {
                String contentType = (String)Headers["content-type"];
                if (contentType != null)
                {
                    foreach (String s in contentType.Split(';'))
                    {
                        String ct = s.Trim().ToLower();
                        if (ct.StartsWith("charset="))
                            return ct.Substring("charset=".Length).Trim();
                    }
                }
                return null;
            }
        }

        public Object this[Object key]
        {
            get
            {
                return _data != null && _data.ContainsKey(key) ? _data[key] : null;
            }
            set
            {
                if (_data == null)
                    _data = new Dictionary<Object, Object>();
                _data[key] = value;
            }
        }

        public String GetParameter(String name)
        {
            ParseParameters();
            if (_parameters.ContainsKey(name))
            {
                Object o = _parameters[name];
                if (o is IList<String>)
                    o = ((IList<String>)o)[0];
                return (String)o;
            }
            else
                return null;
        }

        public String[] GetParameters(String name)
        {
            ParseParameters();
            String[] ret = null;
            if (_parameters.ContainsKey(name))
            {
                Object o = _parameters[name];
                if (o is List<String>)
                    ret = ((List<String>)o).ToArray();
                else
                    ret = new String[] { (String)o };
            }
            else
                ret = new String[0];
            return ret;
        }

        private void ParseParameters()
        {
            if (_parameters != null)
                return;

            Encoding encoding;
            String charset = CharacterEncoding;
            if (charset == null)
                encoding = Encoding.UTF8;
            else
                encoding = Encoding.GetEncoding(charset);

            _parameters = new Dictionary<String, Object>();

            if (QueryString != null)
            {
                ParseQueryString(_parameters, QueryString, encoding);
            }

            // TODO parse form data
        }

        private void ParseQueryString(IDictionary<String, Object> parameters, String query, Encoding e)
        {
            foreach (String s in query.Split('&'))
            {
                ParseParameter(parameters, s, e);
            }
        }

        private void ParseParameter(IDictionary<String, Object> parameters, String s, Encoding e)
        {
            if (String.IsNullOrEmpty(s))
                return;
            Int32 offset = s.IndexOf('=');
            String name, value;
            if (offset == -1)
            {
                name = s;
                value = String.Empty;
            }
            else
            {
                name = s.Substring(0, offset);
                value = s.Substring(offset + 1);
            }
            AddParameter(parameters, name, value);
        }

        private void AddParameter(IDictionary<String, Object> parameters, String key, String value)
        {
            if (parameters.ContainsKey(key))
            {
                Object o = parameters[key];
                IList<String> list = o as IList<String>;
                if (list == null)
                {
                    list = new List<String>();
                    list.Add((String)o);
                    parameters[key] = list;
                }
                list.Add(value);
            }
            else
                parameters[key] = value;
        }
    }

    class RemotingHttpResponse : IHttpResponse
    {
        private Stream _outputStream = new MemoryStream();
        private ITransportHeaders _headers = new TransportHeaders();

        public Stream OutputStream
        {
            get { return _outputStream; }
        }

        public ITransportHeaders Headers
        {
            get { return _headers; }
        }

        public void AppendHeader(String name, String value)
        {
            Headers[name] = value;
        }

        public Int32 StatusCode
        {
            get { return (Int32)_headers["__HttpStatusCode"]; }
            set { _headers["__HttpStatusCode"] = value; }
        }

        public String StatusDescription
        {
            get { return (String)_headers["__HttpReasonPhrase"]; }
            set { _headers["__HttpReasonPhrase"] = value; }
        }
    }
}
