# C#.NET Core Support for Kabomu Library

Retain minimal ideas needed to support sending streams.

For now support only those who provide at most once delivery, ie provide duplicate protection.

## Features

1. Byte streams which preserve message chunk boundaries
2. Stop and wait for ack. Multiplexing at apache thrift and zeromq levels will make use of windows for multiple sequential delivery unnecessary. This greatly simplifies flow control.
3. No retry - to leverage duplicate protection, and sidestep decisions on when to retry and for how long.
4. No addressing - use for internal networks with preallocated "connections" (udp or tcp). Could still have addresses by making it a parameter to pass around, but it will be transparent to stream transfer protocol.
5. Stream id itself is 8 byte int of two types: those uniquely generated at sender and those uniquely generated at receiver. It can be random or sequential and can even be reused at any time, once it is determined that all prior usages are completed or should be cancelled.
6. So fields are: stream id, version, payload (can be empty), pdu type, flags (has more, started at receiver), error code.
7. We must provide beginreceive API for create a stream which returns the stream id. This ensures the receiver of a stream id can immediately retrieve the corresponding stream contents.
8. Send API must indicate whether stream has already being started by receiver or not.
9. Add send and receive timeout options to clear out abandoned transfers - default and custom.
10. Will still need async msg sources and msg sinks, and msg sink factory.
11. Accept cancellation handle or api for beginreceive and beginsend
12. Add beginreset api.

## Mission

Overall mission is toward monolithic applications for enforcement of architecture and better preparation for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable (or immutable but potentially serializable) input and output parameters, to prepare for transportation of messages across a network
4. quasi web requests, to provide alternative request-response protocols like http, and also to ease transition to http usage
5. quasi web mail, for deferred processing
