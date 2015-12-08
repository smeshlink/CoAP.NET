---
layout: article
title: Getting Started
excerpt: An overview of CoAP.NET, how to install and use.
started: true
permalink: /getting-started/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## Installing CoAP.NET

CoAP.NET is available on [NuGet](https://www.nuget.org/packages/CoAP/).

{% highlight powershell %}
PM> Install-Package CoAP
{% endhighlight %}

Or download pre-compiled binaries from [releases page](https://github.com/smeshlink/CoAP.NET/releases/latest).

<small>Currently v{{ site.version }}.</small>

## Using CoAP Client

{% highlight csharp %}
// create a new client
var client = new CoapClient();

// set the Uri to visit
client.Uri = new Uri("coap://SERVER_ADDRESS/helloworld");

// now send a GET request to say hello~
var response = client.Get();

Console.WriteLine(response.PayloadString);  // Hello World!
{% endhighlight %}

[`view example`]({{ site.baseurl }}/examples/client/)

## Using CoAP Server

First, creates a new resource of your own:

{% highlight csharp %}
class HelloWorldResource : Resource
{
	// use "helloworld" as the path of this resource
	public HelloWorldResource() : base("helloworld")
	{
		// set a friendly title
		Attributes.Title = "GET a friendly greeting!";
	}
	
	// override this method to handle GET requests
	protected override void DoGet(CoapExchange exchange)
	{
		// now we get a request, respond it
		exchange.Respond("Hello World!");
	}
}
{% endhighlight %}

Then, starts a new CoAP server and add the new resource:

{% highlight csharp %}
// create a new server
var server = new CoapServer();

// add the resource to share
server.Add(new HelloWorldResource());

// let the server fly
server.Start();
{% endhighlight %}

Now the resource is gettable on `coap://SERVER_ADDRESS/helloworld`.

[`view example`]({{ site.baseurl }}/examples/server/)

## Building from Source

A few compile symbols are introduced to build for different drafts of CoAP:

* `COAP03` -- [draft-ietf-core-coap-03](http://tools.ietf.org/html/draft-ietf-core-coap-03)
* `COAP08` -- [draft-ietf-core-coap-08](http://tools.ietf.org/html/draft-ietf-core-coap-08)
* `COAP12` -- [draft-ietf-core-coap-12](http://tools.ietf.org/html/draft-ietf-core-coap-12)
* `COAP13` -- [draft-ietf-core-coap-13](http://tools.ietf.org/html/draft-ietf-core-coap-13)
* `COAP18` -- [draft-ietf-core-coap-18](http://tools.ietf.org/html/draft-ietf-core-coap-18)
* `RFC7252` -- [RFC7252](http://tools.ietf.org/html/rfc7252)
* `COAPALL` -- all supported drafts above

By default (without any symbol defined), CoAP.NET will be compiled with the latest version of CoAP protocol.
To enable drafts, define one or more of those compile symbols.

With drafts enabled, an interface `ISpec` will be introduced, representing draft specification.
Define `COAPXX` to enable draft XX, or `COAPALL` to enable all supported drafts.
