---
layout: article
title: CoAP Server Example
excerpt: Using CoAP server to provide CoAP resources to remote clients.
example_server: true
prev_section: client
permalink: /examples/server/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## Intro

CoAP sessions are considered as request-response pairs.
Available handlers for requests are defined as `Resource`s.
Each resource can be added to a `CoapServer` and process certain types of requests.

<hr class="soften"/>

## Creating Resource

All resources must implement the `IResource` interface.
Extending the `Resource` class is a good start point.

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

Now this `HelloWorldResource` will handle GET requests from clients
and respond them with a "Hello World!" string.

To handle other types of request, i.e., POST, PUT and DELETE, override
related methods.

{% highlight csharp %}
class HelloWorldResource : Resource
{
	public HelloWorldResource() : base("helloworld")
	{
	}
	
	// override this method to handle GET requests
	protected override void DoGet(CoapExchange exchange)
	{
	}
	
	// override this method to handle POST requests
	protected override void DoPost(CoapExchange exchange)
	{
	}
	
	// override this method to handle PUT requests
	protected override void DoPut(CoapExchange exchange)
	{
	}
	
	// override this method to handle DELETE requests
	protected override void DoDelete(CoapExchange exchange)
	{
	}
}
{% endhighlight %}

If not overrided, request will be responded with a 4.05 (Method Not Allowed).

<hr class="soften"/>

## Creating CoAP Server

### Prepare a server

{% highlight csharp %}
// create a new server
var server = new CoapServer();
{% endhighlight %}

Or you may specify the port(s) to listen to:

{% highlight csharp %}
// create a new server on these ports
var server = new CoapServer(5683, 5684);
{% endhighlight %}

To override default configurations, create your own `ICoapConfig`
and pass it to the server.

{% highlight csharp %}
// define custom configurations
ICoapConfig config = new CoapConfig();
// ...

// create a new server with custom config
var server = new CoapServer(config);
{% endhighlight %}

### Add resources

{% highlight csharp %}
// add the resource to share
server.Add(new HelloWorldResource());
{% endhighlight %}

### Remove a resource

{% highlight csharp %}
server.Remove(...);
{% endhighlight %}

### Start a server

{% highlight csharp %}
server.Start();
{% endhighlight %}

### Stop a server

{% highlight csharp %}
server.Stop();
{% endhighlight %}

<hr class="soften"/>

## Observable Resource

Resources can be enabled as observable simply by setting `Observable` to `true`.
Call `Changed();` whenever a new notification is ready to broadcast.
After this call, the `DoGet` will be called again for each subscriber.

{% highlight csharp %}
class TimeResource : Resource
{
	Timer _timer;
	DateTime _now;

	// use "time" as the path of this resource
	public TimeResource() : base("time")
	{
		// set a friendly title
		Attributes.Title = "GET the current time";
		
		// mark as observable
		Observable = true;
		
		_timer = new Timer(Timed, null, 0, period);
	}
	
	private void Timed(Object o)
	{
		_now = DateTime.Now;
		
		// notify subscribers
		Changed();
	}
	
	protected override void DoGet(CoapExchange exchange)
	{
		exchange.Respond(_now.ToString());
	}
}
{% endhighlight %}

<hr class="soften"/>

See [CoAP Example Server](https://github.com/smeshlink/CoAP.NET/tree/master/CoAP.Example/CoAP.Server) for more.
