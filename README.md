CoAP.NET - A CoAP framework in C#
=================================

[![Build Status](https://api.travis-ci.org/smeshlink/CoAP.NET.png)](https://travis-ci.org/smeshlink/CoAP.NET)

The Constrained Application Protocol (CoAP) (https://datatracker.ietf.org/doc/draft-ietf-core-coap/)
is a RESTful web transfer protocol for resource-constrained networks and nodes.
CoAP.NET is an implementation in C# providing CoAP-based services to .NET applications. 
Reviews and suggestions would be appreciated.

Content
-------
- [Quick Start] (#quick-start)
- [Build] (#build)
- [License] (#license)
- [Acknowledgements] (#acknowledgements)

Quick Start
-----------

CoAP sessions are considered as request-response pair.

### CoAP Client

Access remote CoAP resources by issuing a **[Request] (CoAP.NET/Request.cs)**
and receive its **[Response] (CoAP.NET/Request.cs)**(s).

```csharp
  // new a GET request
  Request request = new Request(Method.GET);
  request.URI = new Uri("coap://[::1]/hello-world");
  request.Send();
  
  // wait for one response
  Response response = request.WaitForResponse();
```

There are 4 types of request: GET, POST, PUT, DELETE, defined as
<code>Method.GET</code>, <code>Method.POST</code>, <code>Method.PUT</code>,
<code>Method.DELETE</code>.

Responses can be received in two ways. By calling <code>request.WaitForResponse()</code>
a response will be received synchronously, which means it will 
block until timeout or a response is arrived. If more responses
are expected, call <code>WaitForResponse()</code> again.

To receive responses asynchronously, register a event handler to
the event <code>request.Respond</code> before executing.

> #### Parsing Link Format
> Use <code>LinkFormat.Parse(String)</code> to parse a link-format
  response. The returned enumeration of <code>WebLink</code>
  contains all resources stated in the given link-format string.
> ```csharp
  Request request = new Request(Method.GET);
  request.URI = new Uri("coap://[::1]/.well-known/core");
  request.Send();
  Response response = request.WaitForResponse();
  IEnumerable<WebLink> links = LinkFormat.Parse(response.PayloadString);
  ```

See [CoAP Example Client] (CoAP.Client) for more.

### CoAP Server

A new CoAP server can be easily built with help of the class
[**CoapServer**] (CoAP.NET/Server/CoapServer.cs)

```csharp
  static void Main(String[] args)
  {
    CoapServer server = new CoapServer();
    
    server.Add(new HelloWorldResource("hello"));
    
    server.Start();
    
    Console.ReadKey();
  }
```

See [CoAP Example Server] (CoAP.Server) for more.

### CoAP Resource

CoAP resources are classes that can be accessed by a URI via CoAP.
In CoAP.NET, a resource is defined as a subclass of [**Resource**] (CoAP.NET/Server/Resources/Resource.cs).
By overriding methods <code>DoGet</code>, <code>DoPost</code>,
<code>DoPut</code> or <code>DoDelete</code>, one resource accepts
GET, POST, PUT or DELETE requests.

The following code gives an example of HelloWorldResource, which
can be visited by sending a GET request to "/hello-world", and
respones a plain string in code "2.05 Content".

```csharp
  class HelloWorldResource : Resource
  {
      public HelloWorldResource()
          : base("hello-world")
      {
          Attributes.Title = "GET a friendly greeting!";
      }

      protected override void DoGet(CoapExchange exchange)
      {
          exchange.Respond("Hello World from CoAP.NET!");
      }
  }
  
  class Server
  {
      static void Main(String[] args)
      {
          CoapServer server = new CoapServer();
          server.Add(new HelloWorldResource());
          server.Start();
      }
  }
```

See [CoAP Example Server] (CoAP.Server) for more.

Build
-----

A few compile symbols are introduced to build for different drafts of
CoAP:

- COAP03  -- [draft-ietf-core-coap-03] (http://tools.ietf.org/html/draft-ietf-core-coap-03)
- COAP08  -- [draft-ietf-core-coap-08] (http://tools.ietf.org/html/draft-ietf-core-coap-08)
- COAP12  -- [draft-ietf-core-coap-12] (http://tools.ietf.org/html/draft-ietf-core-coap-12)
- COAP13  -- [draft-ietf-core-coap-13] (http://tools.ietf.org/html/draft-ietf-core-coap-13)
- COAP18  -- [draft-ietf-core-coap-18] (http://tools.ietf.org/html/draft-ietf-core-coap-18)
- COAPALL -- all supported drafts above

By default (with no symbol defined), CoAP.NET will be compiled with
the latest version of CoAP protocol. To enable drafts, define one or
more of those compile symbols.

With drafts enabled, an interface <code>ISpec</code> will be introduced,
representing draft specification. Define COAPXX to enable draft XX,
or COAPALL to enable all supported drafts. All enabled drafts will be
available in class [**Spec**] (CoAP.NET/Spec.cs):

```csharp
  public static class Spec
  {
    public static readonly ISpec Draft03;
    public static readonly ISpec Draft08;
    public static readonly ISpec Draft12;
    public static readonly ISpec Draft13;
    public static readonly ISpec Draft18;
  }
```

With none of the symbols defined, only the latest version of draft
will be compiled as the class [**Spec**] (CoAP.NET/Spec.cs),
with static members instead of various drafts:

```csharp
  public static class Spec
  {
    public static readonly String Name = "draft-ietf-core-coap-18";
    public static readonly Int32 DefaultPort = 5683;
    public static IMessageEncoder NewMessageEncoder();
    public static IMessageDecoder NewMessageDecoder(Byte[] data);
    public static Byte[] Encode(Message msg);
    public static Message Decode(Byte[] bytes);
  }
```

License
-------

See [LICENSE] (LICENSE) for more info.

Acknowledgements
----------------

CoAP.NET is based on [**Californium**] (https://github.com/mkovatsc/Californium),
a CoAP framework in Java by Matthias Kovatsch, Dominique Im Obersteg,
and Daniel Pauli, ETH Zurich. See <http://people.inf.ethz.ch/mkovatsc/californium.php>.
Thanks to the authors and their great job.
