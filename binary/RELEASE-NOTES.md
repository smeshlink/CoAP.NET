CoAP.NET Release Notes
======================

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
