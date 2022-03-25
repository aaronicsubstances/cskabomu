# C#.NET Core Support for Kabomu Library

Retain minimal ideas needed to support sending streams - expected and unexpected.
For now support only those who provide at most once delivery, ie provide duplicate protection 
• Byte streams which preserve message chunk boundaries
• Stop and wait for ack. Multiplexing at apache thrift and zeromq levels will make use of windows unnecessary. Thus simplifies flow control.
• No retry - to leverage duplicate protection, and sidestep decisions on when to retry and for how long.
• No addressing - use for internal networks with preallocated connections (udp or tcp)
• Stream id itself is random 8 bytes.
• So fields are: stream id (external), version, payload (can be empty), pdu type, flags (is last, must already exist), error code.
• We must provide beginreceive API for create a stream which returns the stream id. This ensures the receiver of a stream id can immediately retrieve the corresponding stream contents.
• Send API must indicate whether stream should be created or not. And can pass in a stream id and bypass internal id generation 
• Add send and receive timeout options to clear out abandoned transfers - default and custom.
• Will still need async msg sources and msg sinks, and msg sink factory.
• Accept cancellation handle or api for beginreceive and beginsend
• Add beginreset api.
• 
Overall mission for monolithic applications is enforcement of architecture and better preparation for evolution to distributed systems, through extending powers of local procedure call to quasi (network involved) procedure calls through: input and output stream parameters (to fully prepare for all kinds of message passing), serializable (or immutable but potentially serializable) input and output parameters, quasi web requests (for possible future replacement with http), and quasi web mail (for deferred processing).