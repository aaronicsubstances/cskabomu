# Introduction

1. Kabomu Library is a message passing framework for building networking protocols for distributed systems.
2. The term "message" as used in Kabomu denotes a stream of bytes which is usually assumed to be bounded. Kabomu seeks to support frameworks which already implement support for exchanging bounded messages, by implementing support for exchanging unbounded messages as well.
3. All protocols based on the framework support both explicitly acked one-way request (for the purpose of data transfer), and implicitly acked request-response message exchange patterns.
4. Some protocols work with immediate processing while others work with deferred processing.
5. Protocols which employ immediate processing are "semantically compatible" with HTTP/1.1, so that terminology of request methods, response status codes, headers, and chunked request and response bodies apply. They differ only in their underlying transports, which are not limited to TCP.
   1. As such such protocols are said to belong to "Quasi-HTTP" protocol family.
6. Protocols which employ deferred processing are "semantically compatible" with a subset of JMAP Mail. They differ only in their underlying transports, which are not limited to HTTP. Deferred processing will be done by automated email thread processors. 
7. Since webmail makes it possible to send email via HTTP, point 6 can still be seen from the alternative perspective of HTTP. This will require use of "dictionary of callbacks" idea for simulating deferred processing as immediate processing (callbacks are invoked with error of "not done yet" after some timeout to signal that request is really being processed in a deferred manner).
8. From investigating RINA research, my opinion is that it is not useful to have long lived connections or flows which have to tolerate idleness or even restarts of peers. Rather I propose that it is enough to have unbounded message exchanges which neither tolerate idleness nor restarts of peers, and scale better horizontally. Thus I had to drop RINA's concepts, and instead continue to emulate success of HTTP's stateless request-response model.

## Design Considerations of Quasi-HTTP

The deployment environments of Kabomu are the Internet via TCP/TLS, and within the same computing host via IPC mechanisms. Kabomu seeks to create few protocols in order to
   1. leverage the greater efficiency made possible by intrahost communications for request-response protocols involving unbounded messages, similar to HTTP. In other words, use of TCP on localhost for HTTP is not necessary when faster and more memory efficient alternatives exist.
   2. simplify socket programming by increasing robustness of persistent TCP connections.

Concerning request-response protocols: Most HTTP libraries out there assume (rightly) that the underlying protocol is TCP, and so do not abstract away the underlying transport for possible use with IPC mechanisms.

Also it is my observation that for communications not involving Web browsers, HTTP itself is strictly needed only in its semantics and programming API, and not its syntax. Thus new protocols (aka Quasi-HTTP protocols) can be developed for implementing a subset of HTTP semantics, but with different syntax, especially with different underlying transports other than TCP.

Concerning simplicity of socket programming: Programming with persistent TCP connections is complicated by the fact that it is not possible to determine whether a TCP connection is dead (due to idleness, long usage or restarts) unless one tries to write to it. However, the tentative solution of retrying a TCP connection to send a message when a write error occurs on a persistent connection faces a problem: *the TCP error is unable to indicate whether or not the message whose sending detected the deadness of the connection was processed.* In practice schemes to keep the TCP connection alive are used, and this is where socket programming becomes complicated. In fact, so complicated that HTTP opted for the simplest solution of creating a new connection for every request.

Fortunately, Quasi-HTTP provides cases in which one can be assured of duplicate protection when sending a message. Kabomu leverages these cases to provide TCP Connection Pooling protocols which can serve as alternative transports to TCP in addition to IPC mechanisms.

Quasi-HTTP's design seeks to retain HTTP's resemblance to making local procedure calls, since both are all request-response protocols. However when communication goes beyond a single host (computer), clients must be aware of certain challenges which are absent when making local procedure calls.

1. A message may experience significant delays during its transmission, so that the usual style of blocking for responses during local procedure call may be too inefficient. This suggests that clients use non-blocking style (e.g. through callbacks), and have timeouts for efficient utilization of computing resources.
2. A request or its response may be lost or destroyed in the network (e.g. due to a network disconnection), and hence will never reach its intended destination. Hence clients must be prepared to deal with the situation where during a timeout, one cannot know (at least immediately) whether the request was processed or not.
3. A message may be modified accidentally or maliciously by the network before reaching its intended destination, so that destination gets a corrupted or wrong message. Hence clients must be prepared to deal with matters of network security.
3. A message may be larger than what a network is capable of transmitting at once, or larger than what the receiving end is capable of processing at once. Hence clients must be prepared to fragment some of their messages for transmission, and reassemble the fragments on delivery.
    1. To prevent clients from having to deal with uncertainty about the safest minimum message size to use, Kabomu offers clients a minimum value of **30KB**. Hence all underlying transports intended for use by Quasi-HTTP must be prepared to send messages of this size. Beyond this limit, clients must be prepared to fragment and reassemble messages on their own.


## Protocols

### Unbounded Message Transfer

1. Byte streams which preserve message chunk boundaries
2. Stop and wait for ack.
3. No retry - to leverage duplicate protection, and sidestep decisions on when to retry and for how long.
4. No addressing - instead pass around opaque connection handles.
5. Stream id itself is 8 byte int of two types: those uniquely generated at sender and those uniquely generated at receiver. It can be random or sequential and can even be reused at any time, once it is determined that all prior usages are completed or should be cancelled.
6. So fields are: stream id, version, payload (can be empty), pdu type, flags (has more, started at receiver), error code.
7. We must provide beginreceive API for create a stream which returns the stream id. This ensures the receiver of a stream id can immediately retrieve the corresponding stream contents.
8. Send API must indicate whether stream has already being started by receiver or not.
9. Add send and receive timeout options to clear out abandoned transfers - default and custom.
10. Will still need async msg sources and msg sinks, and msg sink factory.
11. Accept cancellation handle or api for beginreceive and beginsend
12. Add beginreset api.

### Quasi-HTTP

1. Uses unbounded message transfer protocol to send request as a single message, and to receive response as another message.

2. Request and response headers are sent as a separate first chunk in CSV format (each row starts with header name, followed by its multiple values), and the request and response bodies (if any) follow in the subsequent chunks.

3. All headers without "X-" prefix are reserved. Hence clients must prefix headers with "X-".

3. Special request header is used to indicate the stream id by which receiver can retrieve response with beginreceive API, and also by which send can send response by indicating that response stream has been started by receiver.

### Connection Handle

For connectionless transports, connection handles will be addresses of transport endpoints.

For connection-oriented transports (ie TCP) connection handles will be either addresses or actual TCP connections.

However when load balancing is involved, a Quasi-HTTP call uses the same TCP connection for all its message transsers (for connectionless transports, load balancing cannot be used at all).

### TCP Connection Pooling With Load Balancing

1. Intended for use as a faster underlying transport for Quasi-HTTP on the Cloud (ie on the Internet and with load balancing possibly involved).
 
1. Every send request which doesn't already provide a TCP connection to use, must use a brand new connection, except if message is a first data chunk and has more flag is false and send started at receiver is false. Then in that case protocol tries to use a pooled connection first. If there is a failure, protocol can safely retry sending with a new TCP connection.

3. Where has more is false, or send started at receiver is true, or pdu type is not first data chunk (ie subsequent data chunk or ack chunk), then a TCP connection must be provided, and no retry will be attempted on failure.

4. After a has more is set to false on a first or subsequent data chunk, the connection is pooled, respecting some maximum constraint on pool size.

5. Connections are pooled with sliding and absolute timeout expirations. If *any* of these timeouts fire, connection is evicted.


### TCP Connection Pooling Without Load Balancing

1. Intended for use as a faster underlying transport for Quasi-HTTP across computers on a network (possibly the Internet), when it is known with certainty that there is no load balancing involved.

1. Every request can either be sent with a pooled connection, or with a brand new connection. Usually a pooled connection will be used, unless one is not immediately available.

2. New connections are pooled after first use, respecting some maximum constraint on pool size.

1. Connections are pooled with sliding and absolute timeout expirations. If *any* of these timeouts fire, connection is evicted.

1. Save the time a message is to be sent, but don't send the time during the first time. If a TCP error occurs, create a brand new connection, but add the timestamp when sending the message again. Receiver has to record its restart time, and then upon receiving a timestamp, if it is not definitely after the restart time, should send a specific error code of "restart uncertainty".

3. Add a boolean value for sending with a pooled connection. This value is normally set to true, but anytime a "restart uncertainty" error is encountered, set to false and re-enable after a sliding timeout.

2. To avoid dependence of 32-bit absolute timestamps, make timestamp relative as an unsigned 16-bit unix epoch integer in seconds, modulo 43200. That is, the number of seconds since 12:00 am or 12:00 pm UTC, whichever is closer.
    1. In simple terms this is equivalent to UTC time in 12-hour format, without the AM or PM, and with hour value of 12 equivalent to 0.
    1. To compare two timestamps A and B, the current timestamp is needed. Given current timestamp C, compare negative ((C - A) mod 43200) and negative ((C - B) mod 43200).
    2. The interpretation of the negative quantities being compared is number of seconds ago relative to current timestamp.
    3. E.g. If the current time is 12:00 PM (0), then comparing 10:00 AM (36000) to 2:00 PM (7200) boils down to comparing -7200 with -36000. Since -7200 is bigger, that shows 10:00 AM is "more recent" than 2:00 PM, and hence 2:00 PM was intepreted as belonging to the previous 12-hour cycle, ie as 2:00 AM.
    3. On the other hand if the current time is 6:00 PM (21600), then comparing 10:00 PM (36000) to 2:00 PM (7200) boils down to comparing -28800 with -14400. Since -14400 is bigger, that shows 2:00 PM is "more recent" than 10:00 PM, and hence 10:00 PM was interpreted as belonging to the previous 12-hour cycle, ie as 10:00 AM.
    1. This takes advantage of the fact that the maximum packet lifetimes of physical computer networks in the real world is far less than 1 hour, and hence far far less than 12 hours.
    2. Thus the ambiguity of AM or PM is definitely not a problem, and was done so that 43,200 can fit into a unsigned 16-bit integer. 86,400 would have required more bits. Similarly, seconds was chosen over milliseconds since 43,200,000 would have required even more bits.
