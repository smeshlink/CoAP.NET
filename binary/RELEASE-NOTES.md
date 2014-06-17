CoAP.NET Release Notes
======================

changes in 0.18
---------------

* [added] support for CoAP-18
* [added] new layers and stack structure
* [added] new server and client APIs
* [added] datagram channels for data transmission
* [added] full configuration
* [improved] UDP transmission with SocketAsyncEventArgs for .NET 4+
* [improved] threading with TPL for .NET 4+
* [improved] logging with Common.Logging
* [fixed] lots of bugs

changes in 0.13.4
-----------------

* [fixed] potential breaks in UDP transmission
* [improved] diff assembly title with draft version

changes in 0.13.3
-----------------

* [added] Request.SequenceTimeout to override overall timeout in
  TokenLayer
* [fixed] incorrect match of tokens in TokenManager

changes in 0.13.2
-----------------

* [added] ICommunicator to represent communicators
* [added] ICoapConfig to pass initial variables (refs #8)
* [added] HTTP/CoAP proxy (experimental)
* [added] build for .NET 4.0
* [improved] dispatch requests with thread pool in LocalEndPoint
* [improved] only timeout requests if SequenceTimeout is greater
  than 0 in TokenLayer
* [improved] move resources to separate namespace Resources, and
  add a TimerResource for timed observable resources.

Changes in 0.13.1
------------------

* add timeout and max retransimit to each message
* enable log levels
* fix null reference to next block in TransferLayer

Changes in 0.13
----------------

* update to CoAP-13
* support drafts switching

Version 0.08
-----------

* update to CoAP-08
* support both IPv6/IPv4
