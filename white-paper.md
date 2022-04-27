# Introduction

1. Kabomu Library is a message passing framework for building networking protocols for distributed systems.
2. The term "message" as used in Kabomu denotes a stream of bytes which is usually assumed to be bounded. Kabomu seeks to support frameworks which already implement support for exchanging bounded messages, by implementing support for exchanging unbounded messages as well.
3. All protocols based on the framework support both explicitly acked one-way request (for the purpose of data transfer), and implicitly acked request-response message exchange patterns.
4. Some protocols work with immediate processing while others work with deferred processing.
5. Protocols which employ immediate processing are "semantically compatible" with HTTP/1.1, so that terminology of request methods, response status codes, headers, and chunked request and response bodies apply. They differ only in their underlying transports, which are not limited to TCP.
   1. As such such protocols are said to belong to "Quasi-HTTP" protocol family.
6. Protocols which employ deferred processing are "semantically compatible" with a subset of JMAP Mail. They differ only in their underlying transports, which are not limited to HTTP. Deferred processing will be done by automated email thread processors. 
7. Since webmail makes it possible to send email via HTTP, point 6 can still be seen from the alternative perspective of HTTP. This will require use of "dictionary of callbacks with ttl" idea for simulating deferred processing as immediate processing (callbacks are invoked with error of "not done yet" after some timeout to signal that request is really being processed in a deferred manner).
8. From investigating RINA research, my opinion is that it is not useful to have long lived connections or flows which have to tolerate idleness or even restarts of peers. Rather I propose that it is enough to have unbounded message exchanges which neither tolerate idleness nor restarts of peers, and scale better horizontally. Thus I had to drop RINA's concepts, and instead continue to emulate success of HTTP's stateless request-response model.

## Design Considerations of Quasi-HTTP

The deployment environment of Kabomu is within the same computing host via IPC mechanisms. Kabomu seeks to create Quasi-HTTP in order to leverage the greater efficiency made possible by intrahost communications for request-response protocols involving unbounded messages, similar to HTTP. 

In other words, use of TCP on localhost for HTTP is not necessary when faster and more memory efficient alternatives exist. Most HTTP libraries out there assume (rightly) that the underlying protocol is TCP, and so do not abstract away the underlying transport for possible use with IPC mechanisms.

Also it is my observation that for communications not involving Web browsers, HTTP itself is strictly needed only in its semantics and programming API, and not its syntax. Thus Quasi-HTTP protocols can be developed for implementing a subset of HTTP semantics, but with different syntax, especially with different underlying transports other than TCP.

Quasi-HTTP's design seeks to retain HTTP's resemblance to making local procedure calls, since both are all request-response protocols. However when communication goes beyond a single host (computer), clients must be aware of certain challenges which are absent when making local procedure calls.

1. A message may experience significant delays during its transmission, so that the usual style of blocking for responses during local procedure call may be too inefficient. This suggests that clients use non-blocking style (e.g. through callbacks), and have timeouts for efficient utilization of computing resources.
2. A request or its response may be lost or destroyed in the network (e.g. due to a network disconnection), and hence will never reach its intended destination. Hence clients must be prepared to deal with the situation where during a timeout, one cannot know (at least immediately) whether the request was processed or not.
3. A message may be modified accidentally or maliciously by the network before reaching its intended destination, so that destination gets a corrupted or wrong message. Hence clients must be prepared to deal with matters of network security.
3. A message may be larger than what a network is capable of transmitting at once, or larger than what the receiving end is capable of processing at once. Hence clients must be prepared to fragment some of their messages for transmission, and reassemble the fragments on delivery.
    1. To prevent clients from having to deal with uncertainty about the safest minimum message size to use, Kabomu offers clients a minimum value of **30KB**. Hence all underlying transports intended for use by Quasi-HTTP must be prepared to send messages of this size. Beyond this limit, clients must be prepared to fragment and reassemble messages on their own.