---
layout: article
title: 快速开始
excerpt: CoAP.NET简介、下载与使用。
started: true
permalink: /zh/getting-started/
zh: true
---

# {{ page.title }}

{{ page.excerpt }}

------------------

## 下载 CoAP.NET
{: #installing-coapnet }

通过 [NuGet](https://www.nuget.org/packages/CoAP/) 安装CoAP.NET。

{% highlight powershell %}
PM> Install-Package CoAP
{% endhighlight %}

或者从 [版本页面](https://github.com/smeshlink/CoAP.NET/releases/latest) 下载预编译的二进制文件。

<small>当前版本 v{{ site.version }}.</small>

## 使用 CoAP 客户端
{: #using-coap-client }

{% highlight csharp %}
// 创建一个新的客户端实例
var client = new CoapClient();

// 设置目标访问资源的Uri地址
client.Uri = new Uri("coap://SERVER_ADDRESS/helloworld");

// 发送GET请求
var response = client.Get();

Console.WriteLine(response.PayloadString);  // Hello World!
{% endhighlight %}

[`view example`]({{ site.baseurl }}/examples/client/)

## 使用 CoAP 服务端
{: #using-coap-server }

首先，创建一个新的服务端资源：

{% highlight csharp %}
class HelloWorldResource : Resource
{
	// 设置当前资源的路径为 "helloworld"
	public HelloWorldResource() : base("helloworld")
	{
		// 设置资源的标题
		Attributes.Title = "GET a friendly greeting!";
	}
	
	// 重写 DoGet 方法来处理GET请求
	protected override void DoGet(CoapExchange exchange)
	{
		// 收到一次请求，回复 "Hello World!"
		exchange.Respond("Hello World!");
	}
}
{% endhighlight %}

接下来，将这个资源添加到一个服务端实例，然后启动服务端：

{% highlight csharp %}
// 创建一个新的服务端实例
var server = new CoapServer();

// 添加资源
server.Add(new HelloWorldResource());

// 启动服务端
server.Start();
{% endhighlight %}

现在可以通过 `coap://SERVER_ADDRESS/helloworld` 来访问这个资源了。

[`view example`]({{ site.baseurl }}/examples/server/)

## 构建源码
{: #building-from-source }

CoAP.NET工程定义了一些条件编译符号来构建不同的CoAP版本，支持的版本如下：

* `COAP03` -- [draft-ietf-core-coap-03](http://tools.ietf.org/html/draft-ietf-core-coap-03)
* `COAP08` -- [draft-ietf-core-coap-08](http://tools.ietf.org/html/draft-ietf-core-coap-08)
* `COAP12` -- [draft-ietf-core-coap-12](http://tools.ietf.org/html/draft-ietf-core-coap-12)
* `COAP13` -- [draft-ietf-core-coap-13](http://tools.ietf.org/html/draft-ietf-core-coap-13)
* `COAP18` -- [draft-ietf-core-coap-18](http://tools.ietf.org/html/draft-ietf-core-coap-18)
* `RFC7252` -- [RFC7252](http://tools.ietf.org/html/rfc7252)
* `COAPALL` -- 以上所有的支持版本

默认条件下（不定义任何条件编译符号），CoAP.NET将构建为最新的CoAP版本。
如果需要启用特定版本的CoAP协议，定义相应的条件编译符号即可。
例如，定义`COAP03`将启用CoAP Draft 03。定义`COAPALL`启用所有支持的CoAP版本。

--------

**Next**: [帮助文档]({{ site.baseurl }}/zh/doc/)