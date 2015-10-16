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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using CoAP.Http;
using CoAP.Util;

namespace CoAP.Proxy
{
    public static class HttpTranslator
    {
        private static readonly Dictionary<HttpStatusCode, StatusCode> http2coapCode = new Dictionary<HttpStatusCode, StatusCode>();
        private static readonly Dictionary<StatusCode, HttpStatusCode> coap2httpCode = new Dictionary<StatusCode, HttpStatusCode>();
        private static readonly Dictionary<String, OptionType> http2coapOption = new Dictionary<String, OptionType>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<OptionType, String> coap2httpHeader = new Dictionary<OptionType, String>();
        private static readonly Dictionary<String, Int32> http2coapMediaType = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Int32, String> coap2httpContentType = new Dictionary<Int32, String>();
        private static readonly Dictionary<String, Method> http2coapMethod = new Dictionary<String, Method>(StringComparer.OrdinalIgnoreCase);

        static HttpTranslator()
        {
            http2coapOption["etag"] = OptionType.ETag;
            http2coapOption["accept"] = OptionType.Accept;
            http2coapOption["content-type"] = OptionType.ContentType;
            http2coapOption["cache-control"] = OptionType.MaxAge;
            http2coapOption["if-match"] = OptionType.IfMatch;
            http2coapOption["if-none-match"] = OptionType.IfNoneMatch;
            
            coap2httpHeader[OptionType.IfMatch] = "If-Match";
            coap2httpHeader[OptionType.ETag] = "Etag";
            coap2httpHeader[OptionType.IfNoneMatch] = "If-None-Match";
            coap2httpHeader[OptionType.ContentType] = "Content-Type";
            coap2httpHeader[OptionType.MaxAge] = "Cache-Control";
            coap2httpHeader[OptionType.Accept] = "Accept";
            coap2httpHeader[OptionType.LocationPath] = "Location";
            coap2httpHeader[OptionType.LocationQuery] = "Location";

            http2coapMediaType["text/plain"] = MediaType.TextPlain;
            http2coapMediaType["text/html"] = MediaType.TextHtml;
            http2coapMediaType["image/jpeg"] = MediaType.ImageJpeg;
            http2coapMediaType["image/tiff"] = MediaType.ImageTiff;
            http2coapMediaType["image/png"] = MediaType.ImagePng;
            http2coapMediaType["image/gif"] = MediaType.ImageGif;
            http2coapMediaType["application/xml"] = MediaType.ApplicationXml;
            http2coapMediaType["application/json"] = MediaType.ApplicationJson;
            http2coapMediaType["application/link-format"] = MediaType.ApplicationLinkFormat;

            coap2httpContentType[MediaType.TextPlain] = "text/plain; charset=utf-8";
            coap2httpContentType[MediaType.TextHtml] = "text/html";
            coap2httpContentType[MediaType.ImageJpeg] = "image/jpeg";
            coap2httpContentType[MediaType.ImageTiff] = "image/tiff";
            coap2httpContentType[MediaType.ImagePng] = "image/png";
            coap2httpContentType[MediaType.ImageGif] = "image/gif";
            coap2httpContentType[MediaType.ApplicationXml] = "application/xml";
            coap2httpContentType[MediaType.ApplicationJson] = "application/json; charset=UTF-8";
            coap2httpContentType[MediaType.ApplicationLinkFormat] = "application/link-format";

            http2coapCode[HttpStatusCode.Continue] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.SwitchingProtocols] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.OK] = StatusCode.Content;
            http2coapCode[HttpStatusCode.Created] = StatusCode.Created;
            http2coapCode[HttpStatusCode.Accepted] = StatusCode.Content;
            http2coapCode[HttpStatusCode.NonAuthoritativeInformation] = StatusCode.Content;
            http2coapCode[HttpStatusCode.ResetContent] = StatusCode.Content;
            http2coapCode[HttpStatusCode.PartialContent] = 0;
            http2coapCode[HttpStatusCode.MultipleChoices] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.Moved] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.Redirect] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.RedirectMethod] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.NotModified] = StatusCode.Valid;
            http2coapCode[HttpStatusCode.UseProxy] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.TemporaryRedirect] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.BadRequest] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.Unauthorized] = StatusCode.Unauthorized;
            http2coapCode[HttpStatusCode.PaymentRequired] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.Forbidden] = StatusCode.Forbidden;
            http2coapCode[HttpStatusCode.NotFound] = StatusCode.NotFound;
            http2coapCode[HttpStatusCode.MethodNotAllowed] = StatusCode.MethodNotAllowed;
            http2coapCode[HttpStatusCode.NotAcceptable] = StatusCode.NotAcceptable;
            http2coapCode[HttpStatusCode.Gone] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.LengthRequired] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.PreconditionFailed] = StatusCode.PreconditionFailed;
            http2coapCode[HttpStatusCode.RequestEntityTooLarge] = StatusCode.RequestEntityTooLarge;
            http2coapCode[HttpStatusCode.RequestUriTooLong] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.UnsupportedMediaType] = StatusCode.UnsupportedMediaType;
            http2coapCode[HttpStatusCode.RequestedRangeNotSatisfiable] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.ExpectationFailed] = StatusCode.BadRequest;
            http2coapCode[HttpStatusCode.InternalServerError] = StatusCode.InternalServerError;
            http2coapCode[HttpStatusCode.NotImplemented] = StatusCode.NotImplemented;
            http2coapCode[HttpStatusCode.BadGateway] = StatusCode.BadGateway;
            http2coapCode[HttpStatusCode.ServiceUnavailable] = StatusCode.ServiceUnavailable;
            http2coapCode[HttpStatusCode.GatewayTimeout] = StatusCode.GatewayTimeout;
            http2coapCode[HttpStatusCode.HttpVersionNotSupported] = StatusCode.BadGateway;
            http2coapCode[(HttpStatusCode)507] = StatusCode.InternalServerError;

            coap2httpCode[StatusCode.Created] = HttpStatusCode.Created;
            coap2httpCode[StatusCode.Deleted] = HttpStatusCode.NoContent;
            coap2httpCode[StatusCode.Valid] = HttpStatusCode.NotModified;
            coap2httpCode[StatusCode.Changed] = HttpStatusCode.NoContent;
            coap2httpCode[StatusCode.Content] = HttpStatusCode.OK;
            coap2httpCode[StatusCode.BadRequest] = HttpStatusCode.BadRequest;
            coap2httpCode[StatusCode.Unauthorized] = HttpStatusCode.Unauthorized;
            coap2httpCode[StatusCode.BadOption] = HttpStatusCode.BadRequest;
            coap2httpCode[StatusCode.Forbidden] = HttpStatusCode.Forbidden;
            coap2httpCode[StatusCode.NotFound] = HttpStatusCode.NotFound;
            coap2httpCode[StatusCode.MethodNotAllowed] = HttpStatusCode.MethodNotAllowed;
            coap2httpCode[StatusCode.NotAcceptable] = HttpStatusCode.NotAcceptable;
            coap2httpCode[StatusCode.PreconditionFailed] = HttpStatusCode.PreconditionFailed;
            coap2httpCode[StatusCode.RequestEntityTooLarge] = HttpStatusCode.RequestEntityTooLarge;
            coap2httpCode[StatusCode.UnsupportedMediaType] = HttpStatusCode.UnsupportedMediaType;
            coap2httpCode[StatusCode.InternalServerError] = HttpStatusCode.InternalServerError;
            coap2httpCode[StatusCode.NotImplemented] = HttpStatusCode.NotImplemented;
            coap2httpCode[StatusCode.BadGateway] = HttpStatusCode.BadGateway;
            coap2httpCode[StatusCode.ServiceUnavailable] = HttpStatusCode.ServiceUnavailable;
            coap2httpCode[StatusCode.GatewayTimeout] = HttpStatusCode.GatewayTimeout;
            coap2httpCode[StatusCode.ProxyingNotSupported] = HttpStatusCode.BadGateway;

            http2coapMethod["get"] = Method.GET; ;
            http2coapMethod["post"] = Method.POST;
            http2coapMethod["put"] = Method.PUT;
            http2coapMethod["delete"] = Method.DELETE;
            http2coapMethod["head"] = Method.GET;
        }

        /// <summary>
        /// Gets the CoAP response from an incoming HTTP response. No null value is
	    /// returned. The response is created from a predefined mapping of the HTTP
        /// response codes. If the code is 204, which has
	    /// multiple meaning, the mapping is handled looking on the request method
	    /// that has originated the response. The options are set thorugh the HTTP
	    /// headers and the option max-age, if not indicated, is set to the default
	    /// value (60 seconds). if the response has an enclosing entity, it is mapped
	    /// to a CoAP payload and the content-type of the CoAP message is set
        /// properly.
        /// </summary>
        /// <param name="httpResponse">the http response</param>
        /// <param name="coapRequest">the coap response</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TranslationException"></exception>
        public static Response GetCoapResponse(HttpWebResponse httpResponse, Request coapRequest)
        {
            if (httpResponse == null)
                throw ThrowHelper.ArgumentNull("httpResponse");
            if (coapRequest == null)
                throw ThrowHelper.ArgumentNull("coapRequest");

            HttpStatusCode httpCode = httpResponse.StatusCode;
            StatusCode coapCode = 0;

            // the code 204-"no content" should be managed
            // separately because it can be mapped to different coap codes
            // depending on the request that has originated the response
            if (httpCode == HttpStatusCode.NoContent)
            {
                if (coapRequest.Method == Method.DELETE)
                    coapCode = StatusCode.Deleted;
                else
                    coapCode = StatusCode.Changed;
            }
            else
            {
                if (!http2coapCode.TryGetValue(httpCode, out coapCode))
                    throw ThrowHelper.TranslationException("Cannot convert the HTTP status " + httpCode);
            }

            // create the coap reaponse
            Response coapResponse = new Response(coapCode);

            // translate the http headers in coap options
            IEnumerable<Option> coapOptions = GetCoapOptions(httpResponse.Headers);
            coapResponse.SetOptions(coapOptions);

            // the response should indicate a max-age value (CoAP 10.1.1)
            if (!coapResponse.HasOption(OptionType.MaxAge))
            {
                // The Max-Age Option for responses to POST, PUT or DELETE requests
                // should always be set to 0 (draft-castellani-core-http-mapping).
                coapResponse.MaxAge = coapRequest.Method == Method.GET ? CoapConstants.DefaultMaxAge : 0;
            }

            Byte[] buffer = new Byte[4096];
            using (Stream ms = new MemoryStream(buffer.Length), dataStream = httpResponse.GetResponseStream())
            {
                Int32 read;
                while ((read = dataStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                Byte[] payload = ((MemoryStream)ms).ToArray();
                if (payload.Length > 0)
                {
                    coapResponse.Payload = payload;
                    coapResponse.ContentType = GetCoapMediaType(httpResponse.GetResponseHeader("content-type"));
                }
            }

            return coapResponse;
        }

        /// <summary>
        /// Gets the coap request. Creates the CoAP request from the HTTP method and
	    /// mapping it through the properties file. The uri is translated using
	    /// regular expressions, the uri format expected is either the embedded
	    /// mapping (http://proxyname.domain:80/proxy/coapserver:5683/resource
	    /// converted in coap://coapserver:5683/resource) or the standard uri to
	    /// indicate a local request not to be forwarded. The method uses a decoder
	    /// to translate the application/x-www-form-urlencoded format of the uri. The
	    /// CoAP options are set translating the headers. If the HTTP message has an
	    /// enclosing entity, it is converted to create the payload of the CoAP
	    /// message; finally the content-type is set accordingly to the header and to
        /// the entity type.
        /// </summary>
        /// <param name="httpRequest">the http request</param>
        /// <param name="proxyResource"></param>
        /// <param name="proxyingEnabled"></param>
        /// <returns></returns>
        public static Request GetCoapRequest(IHttpRequest httpRequest, String proxyResource, Boolean proxyingEnabled)
        {
            if (httpRequest == null)
                throw ThrowHelper.ArgumentNull("httpRequest");
            if (proxyResource == null)
                throw ThrowHelper.ArgumentNull("proxyResource");

            Method coapMethod;
            if (!http2coapMethod.TryGetValue(httpRequest.Method, out coapMethod))
                throw ThrowHelper.TranslationException(httpRequest.Method + " method not mapped");

            // create the request
            Request coapRequest = new Request(coapMethod);

            // get the uri
            String uriString = httpRequest.RequestUri;
            // remove the initial "/"
            if (uriString.StartsWith("/"))
                uriString = uriString.Substring(1);
            // TODO URLDecode

            // if the uri contains the proxy resource name, the request should be
            // forwarded and it is needed to get the real requested coap server's
            // uri
            // e.g.:
            // /proxy/[::1]:5684/helloWorld
            // proxy resource: /proxy
            // coap server: [::1]:5684
            // coap resource: helloWorld
            Regex regex = new Regex(".?" + proxyResource + ".*");
            if (regex.IsMatch(uriString))
            {
                // find the first occurrence of the proxy resource
                Int32 index = uriString.IndexOf(proxyResource);
                // delete the slash
                index = uriString.IndexOf('/', index);
                uriString = uriString.Substring(index + 1);

                if (proxyingEnabled)
                {
                    // if the uri hasn't the indication of the scheme, add it
                    if (!uriString.StartsWith("coap://") &&
                        !uriString.StartsWith("coaps://"))
                    {
                        uriString = "coap://" + uriString;
                    }

                    // the uri will be set as a proxy-uri option
                    // set the proxy-uri option to allow the lower layers to underes
                    coapRequest.SetOption(Option.Create(OptionType.ProxyUri, uriString));
                }
                else
                {
                    coapRequest.URI = new Uri(uriString);
                }

                // TODO set the proxy as the sender to receive the response correctly
                //coapRequest.PeerAddress = new EndpointAddress(IPAddress.Loopback);
            }
            else
            {
                // if the uri does not contains the proxy resource, it means the
                // request is local to the proxy and it shouldn't be forwarded

                // set the uri string as uri-path option
                coapRequest.UriPath = uriString;
            }

            // translate the http headers in coap options
            IEnumerable<Option> coapOptions = GetCoapOptions(httpRequest.Headers);
            coapRequest.SetOptions(coapOptions);

            // the payload
            if (httpRequest.InputStream != null)
            {
                Byte[] tmp = new Byte[4096];
                MemoryStream ms = new MemoryStream(tmp.Length);
                Int32 read;
                while ((read = httpRequest.InputStream.Read(tmp, 0, tmp.Length)) > 0)
                {
                    ms.Write(tmp, 0, read);
                }
                coapRequest.Payload = ms.ToArray();
                coapRequest.ContentType = GetCoapMediaType(httpRequest.Headers["content-type"]);
            }

            return coapRequest;
        }

        /// <summary>
        /// Gets the coap media type associated to the http content type. Firstly, it looks
	    /// for a predefined mapping. If this step fails, then it
        /// tries to explicitly map/parse the declared mime/type by the http content type.
	    /// If even this step fails, it sets application/octet-stream as
        /// content-type.
        /// </summary>
        /// <param name="httpContentTypeString"></param>
        /// <returns></returns>
        public static Int32 GetCoapMediaType(String httpContentTypeString)
        {
            Int32 coapContentType = MediaType.Undefined;

            // check if there is an associated content-type with the current contentType
            if (!String.IsNullOrEmpty(httpContentTypeString))
            {
                // delete the last part (if any)
                httpContentTypeString = httpContentTypeString.Split(';')[0];

                // retrieve the mapping
                if (!http2coapMediaType.TryGetValue(httpContentTypeString, out coapContentType))
                    // try to parse the media type
                    coapContentType = MediaType.Parse(httpContentTypeString);
            }

            // if not recognized, the content-type should be
            // application/octet-stream (draft-castellani-core-http-mapping 6.2)
            if (coapContentType == MediaType.Undefined)
                coapContentType = MediaType.ApplicationOctetStream;

            return coapContentType;
        }

        /// <summary>
        /// Gets the coap options starting from an array of http headers. The
	    /// content-type is not handled by this method. The method iterates over an
	    /// array of headers and for each of them tries to find a predefined mapping
	    /// if the mapping does not exists it skips the header
	    /// ignoring it. The method handles separately certain headers which are
	    /// translated to options (such as accept or cache-control) whose content
	    /// should be semantically checked or requires ad-hoc translation. Otherwise,
	    /// the headers content is translated with the appropriate format required by
        /// the mapped option.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<Option> GetCoapOptions(NameValueCollection headers)
        {
            if (headers == null)
                throw ThrowHelper.ArgumentNull("headers");

            List<Option> list = new List<Option>();
            foreach (String key in headers.AllKeys)
            {
                OptionType ot;
                if (!http2coapOption.TryGetValue(key, out ot))
                    continue;

                // FIXME: CoAP does no longer support multiple accept-options.
                // If an HTTP request contains multiple accepts, this method
                // fails. Therefore, we currently skip accepts at the moment.
                if (ot == OptionType.Accept)
                    continue;

                // ignore the content-type because it will be handled within the payload
                if (ot == OptionType.ContentType)
                    continue;

                String headerValue = headers[key].Trim();
                if (ot == OptionType.Accept)
                {
                    // if it contains the */* wildcard, no CoAP Accept is set
                    if (!headerValue.Contains("*/*"))
                    {
                        // remove the part where the client express the weight of each
                        // choice
                        headerValue = headerValue.Split(';')[0].Trim();

                        // iterate for each content-type indicated
                        foreach (String headerFragment in headerValue.Split(','))
                        {
                            // translate the content-type
                            IEnumerable<Int32> coapContentTypes;
                            if (headerFragment.Contains("*"))
                                coapContentTypes = MediaType.ParseWildcard(headerFragment);
                            else
                                coapContentTypes = new Int32[] { MediaType.Parse(headerFragment) };

                            // if is present a conversion for the content-type, then add
                            // a new option
                            foreach (int coapContentType in coapContentTypes)
                            {
                                if (coapContentType != MediaType.Undefined)
                                {
                                    // create the option
                                    Option option = Option.Create(ot, coapContentType);
                                    list.Add(option);
                                }
                            }
                        }
                    }
                }
                else if (ot == OptionType.MaxAge)
                {
                    int maxAge = 0;
                    if (!headerValue.Contains("no-cache"))
                    {
                        headerValue = headerValue.Split(',')[0];
                        if (headerValue != null)
                        {
                            Int32 index = headerValue.IndexOf('=');
                            if (!Int32.TryParse(headerValue.Substring(index + 1).Trim(), out maxAge))
                                continue;
                        }
                    }
                    // create the option
                    Option option = Option.Create(ot, maxAge);
                    list.Add(option);
                }
                else
                {
                    Option option = Option.Create(ot);
                    switch (Option.GetFormatByType(ot))
                    {
                        case OptionFormat.Integer:
                            option.IntValue = Int32.Parse(headerValue);
                            break;
                        case OptionFormat.Opaque:
                            option.RawValue = ByteArrayUtils.FromHexStream(headerValue);
                            break;
                        case OptionFormat.String:
                        default:
                            option.StringValue = headerValue;
                            break;
                    }
                    list.Add(option);
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the http request starting from a CoAP request. The method creates
	    /// the HTTP request through its request line. The request line is built with
	    /// the uri coming from the string representing the CoAP method and the uri
	    /// obtained from the proxy-uri option. If a payload is provided, the HTTP
	    /// request encloses an HTTP entity and consequently the content-type is set.
        /// Finally, the CoAP options are mapped to the HTTP headers.
        /// </summary>
        /// <param name="coapRequest">the coap request</param>
        /// <returns>the http request</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TranslationException"></exception> 
        public static WebRequest GetHttpRequest(Request coapRequest)
        {
            if (coapRequest == null)
                throw ThrowHelper.ArgumentNull("coapRequest");

            Uri proxyUri = null;
            try
            {
                proxyUri = coapRequest.ProxyUri;
            }
            catch (UriFormatException e)
            {
                throw new TranslationException("Cannot get the proxy-uri from the coap message", e);
            }

            if (proxyUri == null)
                throw new TranslationException("Cannot get the proxy-uri from the coap message");

            String coapMethod = coapRequest.Method.ToString();
            
            WebRequest httpRequest = WebRequest.Create(proxyUri);
            httpRequest.Method = coapMethod;

            Byte[] payload = coapRequest.Payload;
            if (payload != null && payload.Length > 0)
            {
                Int32 coapContentType = coapRequest.ContentType;
                String contentTypeString;

                if (coapContentType == MediaType.Undefined)
                    contentTypeString = "application/octet-stream";
                else
                {
                    coap2httpContentType.TryGetValue(coapContentType, out contentTypeString);
                    if (String.IsNullOrEmpty(contentTypeString))
                    {
                        contentTypeString = MediaType.ToString(coapContentType);
                    }
                }

                httpRequest.ContentType = contentTypeString;

                Stream dataStream = httpRequest.GetRequestStream();
                dataStream.Write(payload, 0, payload.Length);
                dataStream.Close();
            }

            NameValueCollection headers = GetHttpHeaders(coapRequest.GetOptions());
            foreach (String key in headers.AllKeys)
            {
                httpRequest.Headers[key] = headers[key];
            }

            return httpRequest;
        }

        /// <summary>
        /// Gets the http headers from a list of CoAP options. The method iterates
        /// over the list looking for a translation of each option in the predefined
        /// mapping. This process ignores the proxy-uri and the content-type because
	    /// they are managed differently. If a mapping is present, the content of the
	    /// option is mapped to a string accordingly to its original format and set
        /// as the content of the header.
        /// </summary>
        /// <param name="optionList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NameValueCollection GetHttpHeaders(IEnumerable<Option> optionList)
        {
            if (optionList == null)
                throw ThrowHelper.ArgumentNull("optionList");

            NameValueCollection headers = new NameValueCollection();

            foreach (Option opt in optionList)
            {
                // skip content-type because it should be translated while handling
                // the payload; skip proxy-uri because it has to be translated in a
                // different way
                if (opt.Type == OptionType.ContentType || opt.Type == OptionType.ProxyUri)
                    continue;

                String headerName;
                if (!coap2httpHeader.TryGetValue(opt.Type, out headerName))
                    continue;

                // format the value
                String headerValue = null;
                OptionFormat format = Option.GetFormatByType(opt.Type);
                if (format == OptionFormat.Integer)
                    headerValue = opt.IntValue.ToString();
                else if (format == OptionFormat.String)
                    headerValue = opt.StringValue;
                else if (format == OptionFormat.Opaque)
                    headerValue = ByteArrayUtils.ToHexString(opt.RawValue);
                else
                    continue;

                // custom handling for max-age
                // format: cache-control: max-age=60
                if (opt.Type == OptionType.MaxAge)
                    headerValue = "max-age=" + headerValue;
                headers[headerName] = headerValue;
            }

            return headers;
        }

        /// <summary>
        /// Sets the parameters of the incoming http response from a CoAP response.
	    /// The status code is mapped through the properties file and is set through
	    /// the StatusLine. The options are translated to the corresponding headers
	    /// and the max-age (in the header cache-control) is set to the default value
	    /// (60 seconds) if not already present. If the request method was not HEAD
	    /// and the coap response has a payload, the entity and the content-type are
        /// set in the http response.
        /// </summary>
        public static void GetHttpResponse(IHttpRequest httpRequest, Response coapResponse, IHttpResponse httpResponse)
        {
            if (httpRequest == null)
                throw ThrowHelper.ArgumentNull("httpRequest");
            if (coapResponse == null)
                throw ThrowHelper.ArgumentNull("coapResponse");
            if (httpResponse == null)
                throw ThrowHelper.ArgumentNull("httpResponse");

            HttpStatusCode httpCode;

            if (!coap2httpCode.TryGetValue(coapResponse.StatusCode, out httpCode))
                throw ThrowHelper.TranslationException("Cannot convert the coap code in http status code: " + coapResponse.StatusCode);

            httpResponse.StatusCode = (Int32)httpCode;

            NameValueCollection nvc = GetHttpHeaders(coapResponse.GetOptions());
            // set max-age if not already set
            if (nvc["cache-control"] == null)
                nvc.Set("cache-control", "max-age=" + CoapConstants.DefaultMaxAge);

            foreach (String key in nvc.Keys)
            {
                httpResponse.AppendHeader(key, nvc[key]);
            }

            Byte[] payload = coapResponse.Payload;
            if (payload != null)
            {
                httpResponse.OutputStream.Write(payload, 0, payload.Length);
                String contentType;
                if (coap2httpContentType.TryGetValue(coapResponse.ContentType, out contentType))
                    httpResponse.AppendHeader("content-type", contentType);
            }
        }
    }
}
