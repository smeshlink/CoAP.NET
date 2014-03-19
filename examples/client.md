---
layout: example
title: CoAP Client
excerpt: Access remote CoAP resources.
next_section: server
permalink: /examples/client/
---

## Intro
--------

CoAP sessions are considered as request-response pair.
Remote CoAP resources can be accessed by issuing a <code>Request</code>
and receive its <code>Response</code>(s).

## Prepare requests
-------------------

There are 4 types of request: GET, POST, PUT, DELETE, defined as
<code>Code.GET</code>, <code>Code.POST</code>, <code>Code.PUT</code>,
<code>Code.DELETE</code>. To create a request, pass the type code
as a parameter to the constructor.

### GET request

{% highlight csharp %}
Request request = new Request(Code.GET);
{% endhighlight %}

### POST request

{% highlight csharp %}
Request request = new Request(Code.POST);
{% endhighlight %}

### PUT request

{% highlight csharp %}
Request request = new Request(Code.PUT);
{% endhighlight %}

### DELETE request

{% highlight csharp %}
Request request = new Request(Code.DELETE);
{% endhighlight %}

## Set resource's URI
---------------------

A resource's URI is a string like <code>"coap://127.0.0.1/hello-world"</code>,
representing the address of a remote CoAP resource.

{% highlight csharp %}
request.URI = new Uri("coap://127.0.0.1/hello-world");
{% endhighlight %}

<div class="alert alert-info">
	If you want to discover what resources the remote server has,
	a URL of <code>"coap://127.0.0.1/.well-known/core"</code> might be used.
</div>

## Define options
-----------------

// TODO

## Attach payload
-----------------

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