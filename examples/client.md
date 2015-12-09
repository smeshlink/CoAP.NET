---
layout: article
title: CoAP Client Example
excerpt: Using CoAP client to access remote CoAP resources.
example_client: true
next_section: server
permalink: /examples/client/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## Intro

CoAP sessions are considered as request-response pairs.
Remote CoAP resources can be accessed by issuing a `Request`
and receive its `Response`(s), or you may use the `CoapClient` class for convenience.

<hr class="soften"/>

## Using CoapClient

The `CoapClient` class provides common sync/async methods for accessing CoAP resourses.

### Prepare a client

{% highlight csharp %}
// create a new client
var client = new CoapClient();
{% endhighlight %}

Or you may specify the resource's Uri to visit:

{% highlight csharp %}
// create a new client with a destincation Uri
var client = new CoapClient(new Uri("coap://SERVER_ADDRESS/helloworld"));
{% endhighlight %}

To override default configurations, create your own `ICoapConfig`
and pass it to the client.

{% highlight csharp %}
// define custom configurations
ICoapConfig config = new CoapConfig();
// ...

// create a new client with custom config
var client = new CoapClient(config);
{% endhighlight %}

The `Uri` can be changed anytime:

{% highlight csharp %}
client.Uri = new Uri("coap://SERVER_ADDRESS/helloagain");
{% endhighlight %}

### Sending a request

{% highlight csharp %}
var res = client.Get();

if (res == null)
{
	// timeout
}
else
{
	// success
}
{% endhighlight %}

This method will block until a response is returned,
or return a `null` if no result before timeout.

You may change the time to wait by setting `client.Timeout`.
By default it is `Timeout.Infinite`, in this case the call will not return
until the max number of re-transmissions for a CoAP message is reached.

### Sending an async request

To avoid blocking, use the async method to get notified when a response arrives.

{% highlight csharp %}
client.GetAsync(res =>
{
	// do something with the response
});
{% endhighlight %}

The `res` in the callback will never be `null`.
Another `fail` callback can be given to handles fails like timeout.

{% highlight csharp %}
client.GetAsync(res =>
{
	// success
}, reason =>
{
	// the reason tells why failed
});
{% endhighlight %}

### Sending requests of other types

There are corresponding sync/async methods for each type of request.

{% highlight csharp %}
// perform a POST request
client.Post(...);

// perform a PUT request
client.Put(...);

// perform a DELETE request
client.Delete(...);

// perform a async POST request
client.PostAsync(...);

// perform a async PUT request
client.PutAsync(...);

// perform a async DELETE request
client.DeleteAsync(...);
{% endhighlight %}

### Adding global handlers

You can add event handlers to `client.Respond` event to be notified on every
response comes to this client.
On the other hand, `client.Error` event will be fired whenever a request fails
inside this client.

<hr class="soften"/>

## Using CoAP Request

### Prepare requests

There are 4 types of request: GET, POST, PUT, DELETE, defined as
`Method.GET`, `Method.POST`, `Method.PUT`, `Method.DELETE`.
To create a request, pass the type enum as a parameter to the constructor.

#### GET request

{% highlight csharp %}
Request request = new Request(Method.GET);
{% endhighlight %}

#### POST request

{% highlight csharp %}
Request request = new Request(Method.POST);
{% endhighlight %}

#### PUT request

{% highlight csharp %}
Request request = new Request(Method.PUT);
{% endhighlight %}

#### DELETE request

{% highlight csharp %}
Request request = new Request(Method.DELETE);
{% endhighlight %}

### Set resource's URI

A resource's URI is a string like `coap://SERVER_ADDRESS/hello-world`,
representing the address of a remote CoAP resource.

{% highlight csharp %}
request.URI = new Uri("coap://SERVER_ADDRESS/hello-world");
{% endhighlight %}

<div class="alert alert-info">
	If you want to discover what resources the remote server has,
	a URL of <code>"coap://SERVER_ADDRESS/.well-known/core"</code> might be used.
</div>

### Define options

// TODO

### Attach payload

Payloads are the data sent to the remote resource along with POST or PUT
requests. Content of strings or bytes arrays are accepted.

{% highlight csharp %}
// set a string as payload
request.SetPayload("data from client");

// or set it with specified media type
request.SetPayload("{ 'msg': 'data from client'}", MediaType.ApplicationJson);

// or give a array of bytes directly
request.Payload = new Byte[] { 0x01, 0x02, 0x03 };
{% endhighlight %}

### Send requests

{% highlight csharp %}
request.Send();
{% endhighlight %}

### Wait for responses

Call `WaitForResponse()` to block and wait for a response.
A `null` will be returned if no result before timeout.

{% highlight csharp %}
var response = request.WaitForResponse();

if (response == null)
{
	// timeout
}
else
{
	// success
}
{% endhighlight %}

To avoid blocking, listen to `request.Respond` event to get notified
when a response arrives.

{% highlight csharp %}
request.Respond += (o, e) =>
{
	// success
	Response response = e.Response;
};

request.TimedOut += (o, e) =>
{
	// timeout
};
{% endhighlight %}

<hr class="soften"/>

## Discover

CoAP provides a mechanism of discovery, enabling resources on a server to be
discovered automatically.

### With CoapClient

{% highlight csharp %}
var links = client.Discover();
foreach (var link in links)
{
	// each link represents a resource on server
}
{% endhighlight %}

### With Request

{% highlight csharp %}
Request request = new Request(Method.GET);

// set the discovery Uri
request.URI = new Uri("coap://SERVER_ADDRESS/.well-known/core");

// wait for the result
var response = request.Send().WaitForResponse();

if (response == null)
{
	// may timeout
}
else if (response.ContentFormat == MediaType.ApplicationLinkFormat)
{
	// should be "application/link-format"
	
	var links = LinkFormat.Parse(response.PayloadString);
	// each link represents a resource on server
}
{% endhighlight %}

<hr class="soften"/>

## Observe

Observe mode is a pub/sub extension for CoAP that enables CoAP clients
to "observe" resources, i.e., to retrieve a representation of a resource
and keep this representation updated by the server over a period of time.

### With CoapClient

{% highlight csharp %}
var relation = client.Observe(res =>
{
	// process the response
});
{% endhighlight %}

There are 2 ways to cancel the subscription:

- `relation.ReactiveCancel();`: send a RST when next notification arrives
- `relation.ProactiveCancel();`: send another cancellation request,
with an Observe Option set to 1 (deregister).

### With Request

Send a request with an Observe Option set to 0 to establish a subscription.

{% highlight csharp %}
Request obs = Request.NewGet();

obs.MarkObserve();
{% endhighlight %}

There are 2 ways to cancel the subscription:

- `obs.Cancel();`: send a RST when next notification arrives
- or send another cancellation request as the following code:

{% highlight csharp %}
Request cancel = Request.NewGet();

cancel.SetOptions(obs.GetOptions());  // same options
cancel.MarkObserveCancel();
cancel.Token = obs.Token;  // same token
cancel.Destination = obs.Destination;

cancel.Send();
{% endhighlight %}

### Auto re-registration

A refreshing mechanism is introduced inside CoAP.NET, which will refresh
the subscription from the client once in a while by sending another same
observe request.

The default refreshing time depends on the `MaxAge` option of the last
notification, plus with the `NotificationReregistrationBackoff` config option.
