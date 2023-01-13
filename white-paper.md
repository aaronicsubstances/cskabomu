# Introduction

1. Kabomu Library started out with the ambition of building a message passing framework for building networking protocols which can utilize IPC mechanisms on localhost to achieve performance superior to HTTP and/or TCP, and at the same time be modular and pluggable enough to employ TCP and/or HTTP on distributed systems.
2. In the context of message passing, the term "message" is usually assumed to be a bounded array of bytes. Right from the beginning however, Kabomu sought to go beyond frameworks which already implement support for exchanging bounded messages (e.g. Apache Thrift), by implementing support for exchanging unbounded messages, aka streams.
3. Initially Kabomu wanted to work with any kind of network communications, in particular regardless of whether

   1. it is connection-oriented or connectionless
   2. exchange pattern is request-response or one-way
   1. it acknowledges delivery or not
   2. it provides duplicate protection or not.
   1. load balancing is present or not

4. That proved to be a herculean task, chiefly because it looked so much like re-inventing TCP even on localhost. So in the end had to stick with a design which is close to TCP, but I was still interested in persistent TCP as a means of achieving superior performance over HTTP on localhost.
5. Concerns about the complexity of dealing with TCP connection resets due to idleness and possible need for connection pooling led me to give up on researching persistent TCP further.
6. All of a sudden, it dawned on me that it is no accident that HTTP has turned out to be so successful, *given its support for upload and download of byte streams of unbounded length over transient TCP connections*.
7. I began realising then that what I wanted in Kabomu is a request-response protocol like HTTP which can run over connections other than TCP.
8. I delved into RINA research as it made promises of interest to my goal, i.e. of having a networking library which can run over any network for both connection-oriented and connectionless modes using long lived connections.
9. After investigating the RINA research and considering how it might fare in a load balancing environment, it is my opinion that it is not useful to have long lived connections or flows which have to tolerate idleness or even restarts of peers. Rather I propose that it is enough to have unbounded message exchanges which neither tolerate idleness nor restarts of peers, and which scale better horizontally. Thus I had to drop RINA's concepts, and instead continue to emulate success of HTTP's stateless request-response model.

# Kabomu as a Quasi-HTTP Library

1. All protocols based on the Kabomu framework support the request-response message exchange pattern.
2. But this dose not imply they all produce responses immediately. Some protocols work with immediate processing while others work with deferred processing.
3. Protocols which employ immediate processing are "semantically compatible" with HTTP/1.1, so that terminology of request methods, response status codes, headers, and chunked request and response bodies apply. They differ only in their underlying transports, which are not limited to TCP.
   1. As such such protocols are said to belong to "Quasi-HTTP" protocol family.
6.  Since webmail (e.g. gmail, outlook) makes it possible to send email via HTTP, protocols which employ deferred processing can be seen as "semantically compatible" with HTTP, although via multiple HTTP requests (an example is [JMAP](https://jmap.io) as described at [Wikipedia](https://en.wikipedia.org/wiki/JSON_Meta_Application_Protocol)).
8. For deferred processing which are able to complete quickly enough, they can be simulated as immediate processing by the use of timeouts and a memory cache of callbacks. Simulation then proceeds by creating a callback and dumping it in the cache, sending its key along with the request deferred for processing, setting a timeout on the callback response, and then looking for one of these possibilities:

   1. deferred processing completes before callback timeout. It uses callback key to fetch, and if it is still there, executes callback. End user get a response in time.
   2. deferred processing takes too long, triggering timeout. Application evicts the callback from its cache, and ask end user to come and check on the task later. At that time it becomes obvious that deferred processing is going on.
   3. memory cache evicts the callback before timeout for the reason of controlling its size. if the cache can notify the application of this event, then application can cancel timeout, and notify user of ongoing deferred processing.

9. Thus it can be seen that the request-response exchange pattern is quite versatile, and it will be nice to be able to run HTTP everywhere.

# Design Considerations of Quasi-HTTP

The deployment environment of Kabomu is within the same computing host via IPC mechanisms. Kabomu seeks to create Quasi-HTTP in order to leverage the greater efficiency made possible by intrahost communications for request-response protocols involving unbounded messages, similar to HTTP. 

In other words, use of TCP on localhost for HTTP is not necessary when faster and more memory efficient alternatives exist. Most HTTP libraries out there assume (rightly) that the underlying protocol is TCP, and so do not abstract away the underlying transport for possible use with IPC mechanisms.

Also it is my observation that for communications not involving Web browsers, HTTP itself is strictly needed only in its semantics and programming API, and not its syntax. Thus Quasi-HTTP protocols can be developed for implementing a subset of HTTP semantics, but with different syntax, especially with different underlying transports other than TCP.

Quasi-HTTP's design seeks to retain HTTP's resemblance to making local procedure calls, since both are all request-response protocols. However when communication goes beyond a single host (computer), clients must be aware of certain challenges which are absent when making local procedure calls.

1. A message may experience significant delays during its transmission, so that the usual style of blocking for responses during local procedure call may be inefficient. This suggests that clients use non-blocking style (e.g. through callbacks, promises, futures), and have timeouts for efficient utilization of computing resources.
2. A request or its response may be lost or destroyed in the network (e.g. due to a network disconnection), and hence will never reach its intended destination. Hence clients must be prepared to deal with the situation where during a timeout, one cannot know (at least immediately) whether the request was processed or not.
3. A message may be modified accidentally or maliciously by the network before reaching its intended destination, so that destination gets a corrupted or wrong message. Hence clients must be prepared to deal with matters of network security.
3. A message may be larger than what a network is capable of transmitting at once, or larger than what the receiving end is capable of processing at once. Hence clients must be prepared to fragment some of their messages for transmission, and reassemble the fragments on delivery.

*Kabomu currently relies on underlying transports to deal with all of these networking challenges, even on localhost. Hence the easiest way to connect Kabomu to a transport implementation, is to use IPC mechanisms which provide ordered delivery of messages with duplicate protection, similar to TCP.*
