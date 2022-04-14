# C#.NET Core Support for Kabomu

## Mission

Overall mission is toward monolithic applications for enforcement of architecture and better preparation for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable input and output parameters, to prepare for transportation of messages across a network. 4. quasi web requests, to provide alternative request-response protocols resembling http, and also to ease transition to http usage
5. quasi web mail, for deferred processing thanks to automated email thread processors, and "dictionary of callbacks with ttl" idea for simulating deferred processing as immediate processing.

## Quasi Procedure Call (QPC) Framework

1. Deployment enviroment: localhost

1. QPC transport wrapper: connectionless, datagram.

1. QPC transports: memory, UDP, unix domain socket.

2. Streaming strategy: use temporary regular files.

3. Multithreading strategy: event loop

3. Protocol: Mimicks Sun RPC and HTTP.

3. Protocol syntax: mixture of JSON and binary.
	
5. QpcTransport API
	1. BeginSendPdu(version: byte, pduType: byte, 
			requestId: int, from: str, to: str,
			flags: byte, headerSize: short, headers: byte[],
			body: byte[], Action<Exception> cb): void
			(example flag is: is body file path?)
	1. (the rest are optional and apply only to memory-based transports)
    1. ShouldSerialize(): bool. Sometimes true or false depending on probability setting of transport.
	3. AltBeginSendRequestPdu(requestId: int, from: str, to: str,
		QuasiHttpRequestMessage, Action<Exception> cb): void
	3. AltBeginSendResponsePdu(requestId: int, from: str, to: str,
		QuasiHttpResponseMessage, Action<Exception> cb): void
	3. AltBeginSendFinPdu(requestId: int, Action<Exception> cb): void

6. QuasiHttpRequestMessage structure
    1. Path: string
	2. PathParameters object
	3. map of headers: string key, string value
	4. RequestBodySize: int. can be negative to indicate unknown size.
	4. RequestBodyType: one of application/json, text/plain or application/octet-stream
	4. RequestBody: IMessageSource (ie async FileStream or byte buffer wrapper) or a serializable object. If using with memory-based transports, IMessageSource can have any implementation.
	
7. QuasiHttpResponseMessage structure
    1. StatusCodeIndicatesSuccess: bool
	2. Status Message: string.
	2. map of headers
	3. ResponseBodySize
	4. ResponseBodyType
	5. ResponseBody: object

9. QpcClient API
    1. BeginSend(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void
	1. (can later add helper methods or helper class which only upload and download bodies,
	    and automatically serializes bodies given enough serialization info)
	2. BeginReceivePdu(version: byte, pduType: byte, 
			requestId: int, from: str, to: str,
			flags: byte, headerSize: short, headers: byte[],
			body: byte[]): void
	3. AltBeginReceiveRequestPdu(requestId: int, from: str, to: str,
		QuasiHttpRequestMessage): void
	3. AltBeginReceiveResponsePdu(requestId: int, from: str, to: str,
		QuasiHttpResponseMessage): void
	3. AltBeginReceiveFinPdu(requestId: int): void
	2. BeginReset(Action<Exception> cb)
	3. Timeout prop
	4. EventLoop prop
	6. IQpcTransport prop
	5. IApplicationCallback: interface with method 
	    1. BeginProcess(QuasiHttpRequestMessage, Action<Exception, object>) 
		where object is either HttpResponseMessage or a candidate for ResponseBody. Can be any serializable object if memory-based transport is in use.
	6. prop for temp file system - for creating and destroying files.
	7. prop for random id generator - used in names of files together with a timestamp.
	
2. It is assumed that IApplicationCallback will need a supporting module or static class which statically declares a dictionary. The dictionary will be filled with pairings of path to IPathCallback object.
	1. It is also assumed that IPathCallback will itself delegate its actual work to a statically declared method, through a closure which is passed to default implementations of IPathCallback.
	2. The main job of the closure is to take care of how to call the statically declared method, by casting props of QuasiHttpRequestMessage to specific types, and spreading arguments from ParsedPathParameters for actual work to be done.
	3. The main job of IPathCallback is to take care of serialization concerns. IApplicationCallback deals only with byte streams, unless it receives a 3rd arg from memory-based transports that it should not serialize (the first 2 args are identical to that of IApplicationCallback).
	   1. Ordered serializers for supported response body types will be passed to IPathCallback from IApplicationCallback as a fourth parameter. This will avoid repeating this information every time default IPathCallback is instantiated. In fact a wrapper class understood by IPathCallbacks used by a given IApplicationCallback implementation will be the fourth parameter type.
	   1. Else IPathCallback has to deserialize request for actual work methods, and then serialize response for IApplicationCallback.
	   2. Looks like IPathCallback will have to recreate instances with request path params, request and response bodies changed before and after calling closure, to cater for multithreading concerns of immutability of function arguments.

2. Default Path Callback structure.
    1. request body serialization info
    3. request path parameters serialization info,
    4. response body serialization info
    3. request body type override: string
    4. closure: Action(QuasiHttpRequestMessage, Action<Exception, object> cb)
    4. BeginProcess(QuasiHttpRequestMessage, Action<Exception, object> cb, skipSerialize: bool)

10. Supporting types:
    1. QuasiHttpException. thrown if StatusCodeIndicatesSuccess is false.
	if this error occurs, it will have a reference to the quasi http response message.