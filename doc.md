---
layout: article
title: Documentation
excerpt: Documentation of CoAP.NET.
doc: true
permalink: /doc/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## Messages

CoAP messages are basic transfer units between peers of CoAP,
including clients and servers.
They are of type [Request](#request), [Response](#response)
or [EmptyMessage](#empty-message). A message is typically composed of:

- a message `Type` (CON, NON, ACK or RST)
- a message identifier `ID`
- a `Token` of 0-8 bytes
- a collection of options
- a payload

Process of transmiting a message would be like:

<figure class="highlight"><pre>
- CON with ACK
	    A          B
	1. CON   ->   
	2.       <-   ACK

- CON with RST
	    A          B
	1. CON   ->   
	2.       <-   RST

- CON without ACK
	    A          B
	1. CON   ->
	2. CON 1 ->
	3. CON 2 ->
	4. CON 3 ->
	5. CON 4 ->
	6. Timeout

- NON
	    A          B
	1. NON   ->  (NOP)

- RST
	    A          B
	1. RST   ->  (NOP)
</pre></figure>

A CON message will be retransmited until it is acked or rejected,
or the max number of retransmissions has reached.
By default the max number of retransmissions is defined by `ICoapConfig.MaxRetransmit`.
Set `Message.MaxRetransmit` property to override it.

The initial timeout for the first transmission is determined automatically.
Set `Message.AckTimeout` property to override it.

### Options

Options in a CoAP message are like HTTP headers, but in a more concise way
other than plain text.

Several methods are provided to get or set options of a message.

{% highlight csharp %}
// Gets a sorted list of all options.
IEnumerable<Option> GetOptions();

// Gets all options of the given type.
IEnumerable<Option> GetOptions(OptionType optionType)

// Gets the first option of the specified option type.
Option GetFirstOption(OptionType optionType);

// Checks if this CoAP message has any option of the specified type.
bool HasOption(OptionType type)

// Adds an option.
AddOption(Option option);

// Sets an option and removes all previous ones of the same type.
SetOption(Option opt);
{% endhighlight %}

Most of option types have their own properties/methods for convenience.

{% highlight csharp %}
// Gets or sets the content-format of this message.
int ContentFormat { get; set; }

// Gets or sets the max-age of this message.
long MaxAge { get; set; }

// Adds a ETag option.
AddETag(byte[] opaque);

// and so on...
{% endhighlight %}

### Events

Following events are introduced to monitor the lifecycle of a message:

- `Acknowledged`: fired when a message is acked by peer as received.
- `Rejected`: fired when a message is reset by peer as rejected.
- `TimedOut`: fired if the max number of re-transmissions has reached.
- `Retransmitting`: fired once a message is retransmitting.
- `Cancelled`: fired when a message is cancelled.

## Request

Request represents a CoAP request and has either the type `CON` or `NON`
and one of the method `GET`, `POST`, `PUT` or `DELETE`.
A request must be sent over an <a href="#endpoint">Endpoint</a> to its destination.

After a request is sent, it can wait for a response with a **synchronous** call,
for instance:

{% highlight csharp %}
Request request = new Request(Method.GET);
request.URI = new Uri("coap://example.com:5683/sensors/temperature");

request.Send();

Response response = request.WaitForResponse();
{% endhighlight %}

You can also add a handler to the event `Request.Respond` to receive
a response **asynchronously** when it arrives:

{% highlight csharp %}
Request request = new Request(Method.GET);
request.URI = new Uri("coap://example.com:5683/sensors/temperature");

request.Respond += (Object sender, ResponseEventArgs e) =>
{
	Response response = e.Response;
};

request.Send();
{% endhighlight %}

### Events

- `Respond`: fired when a response arrives.
- `Responding`: occurs when one block of a esponse arrives in a blockwise transfer.
- `Reregistering`: occurs when an observing request is reregistering.

## Response

A `Response` represents a result message to a CoAP request. A response is either
a **piggy-backed** response with type `ACK`, or a **separate** response with type
`CON` or `NON`. Each response has a `StatusCode`.

## Empty Message

An `EmptyMessage` represents an empty CoAP message. An empty message has either
the message type `ACK` or `RST`.

## Endpoint

An `IEndPoint` is bound to a particular IP address and port, multiplexing CoAP
message exchanges between (potentially multiple) clients and servers.
Clients use an endpoint to send requests to a server.
Servers bind resources to one or more endpoints in order for them to be requested
over the network by clients.

## Resources

// TBD

## Logging

// TBD

## Configuration

// TBD

## Layer Stack

// TBD

## Datagram Channel

// TBD

## Threading

// TBD

## Related Topics

- [The CoAP specification](http://tools.ietf.org/html/rfc7252)
- [The CoRE Link Format specification](http://tools.ietf.org/html/rfc6690)
- [Observing Resources in CoAP](http://tools.ietf.org/html/rfc7641)
- [Blockwise transfers in CoAP](http://tools.ietf.org/html/draft-ietf-core-block)
