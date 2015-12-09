---
layout: article
title: CoAP 客户端示例
excerpt: 使用 CoAP 客户端访问远程 CoAP 资源。
example_client: true
zh: true
next_section: server
permalink: /zh/examples/client/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## 简介

CoAP 会话是请求/响应模式。通过发送 `Request` 请求可以访问远程 CoAP 资源，
并获取 `Response` 响应。另外，也可以使用 `CoapClient` 类进行更便捷的操作。

<hr class="soften"/>

## 使用 CoapClient
{: #using-coapclient }

`CoapClient` 类提供了一些访问 CoAP 资源的常用同步/异步方法。

### 准备客户端

{% highlight csharp %}
// 创建一个新的客户端实例
var client = new CoapClient();
{% endhighlight %}

也可以同时指定目标资源的地址：

{% highlight csharp %}
// 创建一个新的客户端实例，指定目标资源地址
var client = new CoapClient(new Uri("coap://SERVER_ADDRESS/helloworld"));
{% endhighlight %}

如果不想使用默认的配置，可以创建新的配置，并传入构造函数。

{% highlight csharp %}
// 自定义配置
ICoapConfig config = new CoapConfig();
// ...

// 创建一个新的客户端实例，使用自定义配置
var client = new CoapClient(config);
{% endhighlight %}

`Uri` 属性表求目标资源地址，可以随时修改。

{% highlight csharp %}
client.Uri = new Uri("coap://SERVER_ADDRESS/helloagain");
{% endhighlight %}

### 发送请求

{% highlight csharp %}
var res = client.Get();

if (res == null)
{
	// 请求超时
}
else
{
	// 请求成功
}
{% endhighlight %}

这是同步方法，将会阻塞当前线程，直到有响应到达，或者超时。
超时时将返回 `null`。

设置 `client.Timeout` 可以修改超时等待时间。
默认值为 `Timeout.Infinite`，此时将等待 CoAP 重传机制的最大时间。

### 发送异步请求

使用异步请求可以避免阻塞调用。

{% highlight csharp %}
client.GetAsync(res =>
{
	// 处理响应
});
{% endhighlight %}

响应处理委托中的 `res` 不会为 `null`。如果要处理超时等异常，可以增加
一个 `fail` 委托。

{% highlight csharp %}
client.GetAsync(res =>
{
	// 请求成功
}, reason =>
{
	// 请求失败，reason 表示失败的原因
});
{% endhighlight %}

### 发送其他类型的请求

每一种类型的请求均有对应的同步/异步方法：

{% highlight csharp %}
// 发送 POST 请求
client.Post(...);

// 发送 PUT 请求
client.Put(...);

// 发送 DELETE 请求
client.Delete(...);

// 发送异步 POST 请求
client.PostAsync(...);

// 发送异步 PUT 请求
client.PutAsync(...);

// 发送异步 DELETE 请求
client.DeleteAsync(...);
{% endhighlight %}

### 全局事件

`client.Respond` 事件可以用于接收进入当前客户端实例的所有请求；
`client.Error` 事件可以用于处理当前客户端实例发生的所有请求失败异常。

<hr class="soften"/>

## 使用 CoAP Request
{: #using-coap-request }

### 准备请求

CoAP 请求有4种类型，分别定义为 `Method.GET`, `Method.POST`, `Method.PUT`, `Method.DELETE`。
创建请求实例时，需要指定请求类型。

#### 创建 GET 请求

{% highlight csharp %}
Request request = new Request(Method.GET);
{% endhighlight %}

#### 创建 POST 请求

{% highlight csharp %}
Request request = new Request(Method.POST);
{% endhighlight %}

#### 创建 PUT 请求

{% highlight csharp %}
Request request = new Request(Method.PUT);
{% endhighlight %}

#### 创建 DELETE 请求

{% highlight csharp %}
Request request = new Request(Method.DELETE);
{% endhighlight %}

### 设置目标资源地址

资源地址的格式为 `coap://[服务端地址]/[资源路径]`。

{% highlight csharp %}
request.URI = new Uri("coap://SERVER_ADDRESS/hello-world");
{% endhighlight %}

<div class="alert alert-info">
	用于资源发现的地址是 <code>"coap://SERVER_ADDRESS/.well-known/core"</code>
</div>

### 设置选项

// TODO

### 添加数据

POST 和 PUT 请求可以携带数据域，包含需要发送到服务端的数据。
数据内容可以是字符串或者一组字节。

{% highlight csharp %}
// 设置字符串内容
request.SetPayload("data from client");

// 设置字符串内容，同时指定内容类型
request.SetPayload("{ 'msg': 'data from client'}", MediaType.ApplicationJson);

// 设置一组字节作为数据内容
request.Payload = new Byte[] { 0x01, 0x02, 0x03 };
{% endhighlight %}

### 发送请求

{% highlight csharp %}
request.Send();
{% endhighlight %}

### 等待响应

调用 `WaitForResponse()` 可以挂起当前线程并等待响应。如果请求超时，将返回 `null`。

{% highlight csharp %}
var response = request.WaitForResponse();

if (response == null)
{
	// 请求超时
}
else
{
	// 响应成功
}
{% endhighlight %}

使用 `request.Respond` 事件可以避免阻塞调用。

{% highlight csharp %}
request.Respond += (o, e) =>
{
	// 响应成功
	Response response = e.Response;
};

request.TimedOut += (o, e) =>
{
	// 请求超时
};
{% endhighlight %}

<hr class="soften"/>

## 资源发现
{: #discover }

CoAP 提供了一种资源发现机制，使客户端可以自动发现服务端提供了哪些可访问资源。

### 使用 CoapClient

{% highlight csharp %}
var links = client.Discover();
foreach (var link in links)
{
	// 每一项 link 表示一个可访问的服务端资源
}
{% endhighlight %}

### 使用 Request

{% highlight csharp %}
Request request = new Request(Method.GET);

// 设置资源发现地址
request.URI = new Uri("coap://SERVER_ADDRESS/.well-known/core");

// 等待响应
var response = request.Send().WaitForResponse();

if (response == null)
{
	// 请求超时
}
else if (response.ContentFormat == MediaType.ApplicationLinkFormat)
{
	// 响应内容类型应为 "application/link-format"
	
	var links = LinkFormat.Parse(response.PayloadString);
	// 每一项 link 表示一个可访问的服务端资源
}
{% endhighlight %}

<hr class="soften"/>

## 资源订阅
{: #observe }

资源订阅是 CoAP 上的通知/订阅 (pub/sub) 功能，CoAP 客户端可以通过订阅
服务端资源，从而持续获取资源的更新状态。

### 使用 CoapClient

{% highlight csharp %}
var relation = client.Observe(res =>
{
	// 处理响应
});
{% endhighlight %}

取消订阅有两种方法：

- `relation.ReactiveCancel();`: 当下一次订阅通知到达时，发送 `RST` 取消订阅
- `relation.ProactiveCancel();`: 主动发送一次取消订阅请求，将 `Observe` 选项设置为1

### 使用 Request

建立订阅的请求是一个 GET 请求，并且将 `Observe` 选项设置为0。

{% highlight csharp %}
Request obs = Request.NewGet();

obs.MarkObserve();
{% endhighlight %}

取消订阅有两种方法：

- `obs.Cancel();`: 当下一次订阅通知到达时，发送 `RST` 取消订阅
- 或者主动发送一次取消订阅请求：

{% highlight csharp %}
Request cancel = Request.NewGet();

cancel.SetOptions(obs.GetOptions());  // 使用相同的 options
cancel.MarkObserveCancel();
cancel.Token = obs.Token;  // 使用相同的 token
cancel.Destination = obs.Destination;

cancel.Send();
{% endhighlight %}

### 订阅自动刷新

CoAP.NET 提供了自动刷新订阅请求的功能，防止意外断开连接。这会定时向服务端重新
发起同样的订阅请求。

默认的刷新间隔为：最后一次通知的 `MaxAge` 选项值 + `NotificationReregistrationBackoff` 配置
