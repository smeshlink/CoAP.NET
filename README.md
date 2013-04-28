CoAP.NET - A CoAP framework in C#
=================================

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
  Request request = new Request(Code.GET);
  request.URI = new Uri("coap://[::1]/hello-world");
  request.Execute();
  
  // receive one response
  Response response = request.ReceiveResponse();
```

There are 4 types of request: GET, POST, PUT, DELETE, defined as
<code>Code.GET</code>, <code>Code.POST</code>, <code>Code.PUT</code>,
<code>Code.DELETE</code>.

Responses can be received in two ways. By calling <code>request.ReceiveResponse()</code>
a response will be received synchronously, which means it will 
block until timeout or a response is arrived. If more responses
are expected, call <code>ReceiveResponse()</code> again.

To receive responses asynchronously, register a event handler to
the event <code>request.Responded</code> before executing.

> #### Parsing Link Format
> Use <code>RemoteResource.NewRoot()</code> to parse a link-format
  response. The returned root resource contains all resources stated
  in the given link-format string.
> ```csharp
  Request request = new Request(Code.GET);
  request.URI = new Uri("coap://[::1]/.well-known/core");
  request.Execute();
  Response response = request.ReceiveResponse();
  Resource root = RemoteResource.NewRoot(response.PayloadString);
  ```

See [CoAP Example Client] (CoAP.Client) for more.

### CoAP Server

A new CoAP server can be easily built by just inheriting the class
[**LocalEndPoint**] (CoAP.NET/EndPoint/LocalEndPoint.cs)

```csharp
  class CoAPServer : LocalEndPoint
  {
  }
  
  static void Main(String[] args)
  {
    CoAPServer server = new CoAPServer();
    Console.WriteLine("CoAP server started on port {0}", server.Communicator.Port);
  }
```

See [CoAP Example Server] (CoAP.Server) for more.

### CoAP Resource

CoAP resources are classes that can be accessed by a URI via CoAP.
In CoAP.NET, a resource is defined as a subclass of [**LocalResource**] (CoAP.NET/EndPoint/LocalResource.cs).
By overriding methods <code>DoGet</code>, <code>DoPost</code>,
<code>DoPut</code> or <code>DoDelete</code>, one resource accepts
GET, POST, PUT or DELETE requests.

The following code gives an example of HelloWorldResource, which
can be visited by sending a GET request to "/hello-world", and
respones a plain string in code "2.05 Content".

```csharp
  class HelloWorldResource : LocalResource
  {
      public HelloWorldResource()
          : base("hello-world")
      {
      }

      public override void DoGet(Request request)
      {
          Response response = new Response(Code.Content);
          response.PayloadString = "Hello World from CoAP.NET!";
          request.Respond(response);
      }
  }
  
  class CoAPServer : LocalEndPoint
  {
      public CoAPServer()
      {
          AddResource(new HelloWorldResource());
      }
  }
```

See [CoAP Example Server] (CoAP.Server) for more.

Build
-----

A few compile symbols are introduced to build for different drafts of
CoAP:

- COAPALL -- all supported drafts below
- COAP03  -- [draft-ietf-core-coap-03] (http://tools.ietf.org/html/draft-ietf-core-coap-03)
- COAP08  -- [draft-ietf-core-coap-08] (http://tools.ietf.org/html/draft-ietf-core-coap-08)
- COAP12  -- [draft-ietf-core-coap-12] (http://tools.ietf.org/html/draft-ietf-core-coap-12)
- COAP13  -- [draft-ietf-core-coap-13] (http://tools.ietf.org/html/draft-ietf-core-coap-13)

With COAPALL defined, all supported drafts will be available in class
[**Spec**] (CoAP.NET/Spec.cs):

```csharp
  public static class Spec
  {
    public static readonly ISpec Draft03;
    public static readonly ISpec Draft08;
    public static readonly ISpec Draft12;
    public static readonly ISpec Draft13;
  }
```

With one of the other symbols defined (i.e. COAP08), only a specific
version of draft will be compiled as the class [**Spec**] (CoAP.NET/Spec.cs),
with constants and static methods instead of various drafts:

```csharp
  public static class Spec
  {
    public const String Name = "draft-ietf-core-coap-08";
    public const Int32 DefaultPort = 5683;
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
