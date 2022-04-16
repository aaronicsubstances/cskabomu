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

3. Protocol syntax: CSV headers and binary body
    1. beginning CSV row will be pduHeader consisting of base64 encoded concatenation of version, pduType, requestId, flags.
    2. ending CSV row will be empty to separate the headers from the body.
    3. user specified headers will be encoded with leading empty string column.
    
5. QpcTransport API
    1. BeginSendPdu(data, offset, length, Action<Exception> cb): void
    1. (the rest are optional and apply only to memory-based transports)
    1. ShouldSerialize(): bool. Sometimes true or false depending on probability setting of transport.
    2. BeginPost(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void

6. QuasiHttpRequestMessage structure
    1. Host: destination.
    2. Origin (Internal): source.
    2. Verb (Internal): must always be POST.
    1. Path: Cannot have query string.
    3. Headers: map of strings to list of strings.
    4. ContentLength: int. can be negative to indicate unknown size.
    4. ContentLocation (Internal): Used when body was streamed to a temp file path.
    4. ContentType: one of application/json (always UTF-8), text/plain (always UTF-8), application/octet-stream, application/x-www-form-urlencoded (always UTF-8).
        1. Body type for HTML Forms is added so as to completely discard need for query string handling in Path, by requiring such query strings to be sent through POST body.
        2. This also means GET with query string has an alternative representation in QuasiHttp.
    4. Body: object. However QPC Client API requires it to be IMessageSource (internally based on async FileStream or byte buffer wrapper)
        1. If using with memory-based transports, object doesn't have to be IMessageSource, but must be serializable.
        2. It is intended that this prop be replaceable by custom request processors.

7. QuasiHttpResponseMessage structure
    1. Host (Internal): destination.
    2. Origin (Internal): source.
    1. IsSuccess: bool.
    1. IsErrorFromClient: bool. false means error is from server if IsSuccess is false too. Ignored if IsSuccess is true.
    2. StatusMessage: string.
    2. Headers
    3. ContentLength
    4. ContentLocation (Internal).
    4. ContentType
    5. Body: object

9. QpcClient API (works for server too, similar to how UdpClient works both ways).
    1. BeginPost(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void
    1. (can later add helper methods or helper class which only upload and download bodies, and automatically serializes bodies given enough serialization info)
    2. BeginReceivePdu(data, offset, length): void
    2. BeginReset(Action<Exception> cb)
    3. Timeout prop
    4. EventLoop prop
    6. IQpcTransport prop
    5. IApplicationCallback: interface with method 
        1. BeginPost(QuasiHttpRequestMessage, Action<Exception, object>) 
        where object is either HttpResponseMessage or a candidate for ResponseBody. Can be any serializable object if memory-based transport is in use.
        2. Serialize(object): byte[]
    6. prop for temp file system - for creating and destroying files.
    7. prop for random id generator - used in names of files together with a timestamp.
    
2. It is assumed that IApplicationCallback will need a supporting module or static class which statically declares a dictionary. The dictionary will be filled with pairings of path to IPathCallback object. IPathCallback has similar method with first 2 args identical to that of IApplicationCallback.
    1. The main job of IPathCallback is to take care of deserialization concerns. It is assumed that mostly serialization can be done generically (and so IApplicationCallback can handle that), but deserialization usually require more context-specific information to succeed (and so IPathCallback is needed). 
    1. Ordered serializers for supported response body types will be stored in IApplicationCallback default implementation, and made available to IPathCallback via IApplicationCallback field reference or method argument
    1. It is assumed that IPathCallback will itself delegate its actual work to a statically declared method, through a closure which is passed to default implementations of IPathCallback.
    2. The main job of the closure is to take care of how to call the statically declared method (or even create instances via dependency injection and call an instance method), by casting props of QuasiHttpRequestMessage to specific types, and spreading arguments from ParsedPathParameters for actual work to be done.
    2. Looks like IPathCallback will have to recreate instances with request path params, request and response bodies changed before and after calling closure, to cater for multithreading concerns of immutability of function arguments.
    3. It's up to more complex IApplicationCallbacks to parse path for embeded  pieces of information, like how REST URLS are structured.

2. Default Path Callback structure.
    1. request body serialization info
    3. request path parameters serialization info,
    4. response body serialization info
    3. request body type override: string
    4. closure: Action(QuasiHttpRequestMessage, Action<Exception, object> cb)
    4. BeginPost(QuasiHttpRequestMessage, Action<Exception, object> cb, IApplicationCallbacks)

10. Supporting types:
    1. QuasiHttpException. thrown if IsSuccess is false.
    if this error occurs, it will have a reference to the quasi http response message.