CoAP.NET - A CoAP framework in C#
=================================

The Constrained Application Protocol (CoAP) is a RESTful web transfer
protocol for resource-constrained networks and nodes. CoAP.NET is an 
implementation in C# providing CoAP-based services to .NET apps. 
Reviews and suggestions would be appreciated.

Quick Start
-----------

CoAP sessions are considered as request-response pair.

### CoAP Client

Access remote CoAP resources by issuing a **Request** and receive its
**Response**(s).

```csharp
  Request request = new Request(Code.GET);
  request.URI = new Uri("coap://[::1]/hello-world");
  request.Execute();
  
  Response response = request.ReceiveResponse();
```

### CoAP Server

A new CoAP server can be easily built by just inheriting the class
**LocalEndPoint**

```csharp
  class SampleServer : LocalEndPoint
  {
  }
  
  static void Main(String[] args)
  {
    SampleServer server = new SampleServer();
    Console.WriteLine("Sample server started on port {0}", server.Communicator.Port);
  }
```

Build
-----

A few compile symbols are introduced to build for different drafts of
CoAP:

* COAPALL -- all supported drafts below
* COAP03  -- [draft-ietf-core-coap-03] (http://tools.ietf.org/html/draft-ietf-core-coap-03)
* COAP08  -- [draft-ietf-core-coap-08] (http://tools.ietf.org/html/draft-ietf-core-coap-08)
* COAP12  -- [draft-ietf-core-coap-12] (http://tools.ietf.org/html/draft-ietf-core-coap-12)
* COAP13  -- [draft-ietf-core-coap-13] (http://tools.ietf.org/html/draft-ietf-core-coap-13)

With COAPALL defined, all supported drafts will be available and can
be found in class **Spec**:

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
version of draft will be compiled as the class **Spec**, with constants
and static methods instead of various drafts:

```csharp
  public static class Spec
  {
    public const String Name = "draft-ietf-core-coap-08";
    public const Int32 DefaultPort = 5683;
    public static Byte[] Encode(Message msg);
    public static Message Decode(Byte[] bytes);
  }
```

Acknowledgements
----------------

CoAP.NET is based on [**Californium**] (https://github.com/mkovatsc/Californium),
a CoAP framework in Java by Matthias Kovatsch, Dominique Im Obersteg,
and Daniel Pauli, ETH Zurich. See <http://people.inf.ethz.ch/mkovatsc/californium.php>.
Thanks to the authors and their great job.
