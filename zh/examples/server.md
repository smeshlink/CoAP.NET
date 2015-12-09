---
layout: article
title: CoAP 服务端示例
excerpt: 建立 CoAP 服务端，为远程客户端提供可访问的 CoAP 资源。
example_server: true
zh: true
prev_section: client
permalink: /zh/examples/server/
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## 简介

CoAP 会话是请求/响应模式。服务端是以资源 `Resource` 的形式提供服务。
每一项资源可以被添加到服务端，并处理特定类型的访问请求。

<hr class="soften"/>

## 创建 CoAP 资源
{: #creating-resource }

CoAP 资源必须实现 `IResource` 接口。最简单的方法是继承 `Resource` 类。

{% highlight csharp %}
class HelloWorldResource : Resource
{
	// 设置当前资源的路径为 "helloworld"
	public HelloWorldResource() : base("helloworld")
	{
		// 设置资源的标题
		Attributes.Title = "GET a friendly greeting!";
	}
	
	// 重写 DoGet 方法来处理 GET 请求
	protected override void DoGet(CoapExchange exchange)
	{
		// 收到一次请求，回复 "Hello World!"
		exchange.Respond("Hello World!");
	}
}
{% endhighlight %}

这个 `HelloWorldResource` 资源可以处理 GET 请求，并回复一个 "Hello World!"。

实现其他方法可以使资源能够处理其他类型的请求：

{% highlight csharp %}
class HelloWorldResource : Resource
{
	public HelloWorldResource() : base("helloworld")
	{
	}
	
	// 重写 DoGet 方法来处理 GET 请求
	protected override void DoGet(CoapExchange exchange)
	{
	}
	
	// 重写 DoPost 方法来处理 POST 请求
	protected override void DoPost(CoapExchange exchange)
	{
	}
	
	// 重写 DoPut 方法来处理 PUT 请求
	protected override void DoPut(CoapExchange exchange)
	{
	}
	
	// 重写 DoDelete 方法来处理 DELETE 请求
	protected override void DoDelete(CoapExchange exchange)
	{
	}
}
{% endhighlight %}

如果没有重写请求类型对应的方法，默认的响应将是：4.05 (Method Not Allowed)

<hr class="soften"/>

## 创建 CoAP 服务端
{: #creating-coap-server }

### 准备服务端

{% highlight csharp %}
// 创建一个新的服务端实例
var server = new CoapServer();
{% endhighlight %}

创建服务端实例时可以指定端口：

{% highlight csharp %}
// 创建一个新的服务端实例，监听指定端口
var server = new CoapServer(5683, 5684);
{% endhighlight %}

如果不想使用默认的配置，可以创建新的配置，并传入构造函数。

{% highlight csharp %}
// 自定义配置
ICoapConfig config = new CoapConfig();
// ...

// 创建一个新的服务端实例，使用自定义配置
var server = new CoapServer(config);
{% endhighlight %}

### 添加资源

{% highlight csharp %}
// 添加资源
server.Add(new HelloWorldResource());
{% endhighlight %}

### 移除资源

{% highlight csharp %}
server.Remove(...);
{% endhighlight %}

### 启动服务端

{% highlight csharp %}
server.Start();
{% endhighlight %}

### 停止服务端

{% highlight csharp %}
server.Stop();
{% endhighlight %}

<hr class="soften"/>

## 资源订阅
{: #observable-resource }

创建可订阅资源时，只需要将 `Observable` 属性设置为 `true` 即可。
当需要广播通知时，调用 `Changed();`，然后每一个订阅客户端将会模拟调用一次 `DoGet`。

{% highlight csharp %}
class TimeResource : Resource
{
	Timer _timer;
	DateTime _now;

	// 设置当前资源的路径为 "time"
	public TimeResource() : base("time")
	{
		// 设置资源的标题
		Attributes.Title = "GET the current time";
		
		// 设置为可订阅资源
		Observable = true;
		
		_timer = new Timer(Timed, null, 0, period);
	}
	
	private void Timed(Object o)
	{
		_now = DateTime.Now;
		
		// 通知订阅者
		Changed();
	}
	
	protected override void DoGet(CoapExchange exchange)
	{
		exchange.Respond(_now.ToString());
	}
}
{% endhighlight %}

<hr class="soften"/>

了解更多请查看 [CoAP Example Server](https://github.com/smeshlink/CoAP.NET/tree/master/CoAP.Example/CoAP.Server)。
