# C#.NET Core Support for Kabomu

Quasi Web Application Framework modelled after ExpressJS, which runs entirely on localhost.

## Mission

Overall mission is toward monolithic applications for enforcement of architecture and better preparation for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable input and output parameters, to prepare for transportation of messages across a network.
4. quasi web requests, to provide alternative request-response protocols resembling http, and also to ease transition to http usage
5. quasi web mail, for deferred processing thanks to automated email thread processors, and "dictionary of callbacks with ttl" idea for simulating deferred processing as immediate processing.

## Design

1. Deployment enviroment: localhost

1. Quasi Web transport wrapper: connectionless, datagram.

1. Quasi Web transports: memory, UDP, unix domain socket.

2. Streaming strategy: use temporary regular files.

3. Multithreading strategy: event loop

3. Web request processing strategy: ExpressJS

3. Protocol: Mimicks Sun RPC and HTTP. Also mimicks HTTP/2 in using headers in place of request line, response line and even scheme (https).

3. Protocol syntax: binary preamble, CSV headers, and binary trailer
    1. preamble is concatenation of version, pduType, requestId, flags, embeddedHttpBodyLen.
    2. CSV is used for http headers and HTML forms. Each CSV row is a key followed by multiple values.
    3. user specified headers must be capitalized or contain an upper case letter (ie cannot contain only lower case letters).

5. IApplicationCallback interface
    1. BeginProcessPost(QuasiHttpRequestMessage, Action<Exception, HttpResponseMessage>)

5. QpcTransport API
    1. BeginSendPdu(data, offset, length, Action<Exception> cb): void
    1. IApplicationCallback property - for processing outgoing requests in transports capable of doing so

6. QuasiHttpRequestMessage structure
    1. host: destination.
    2. verb (Internal): must always be POST.
    1. path
    4. content-length: int. can be negative to indicate unknown size.
    4. content-location (Internal): Used when body was streamed to a temp file path.
    4. content-type: one of application/octet-stream, application/json (always UTF-8), text/plain (always UTF-8), application/x-www-form-urlencoded (always UTF-8).
        1. Body type for HTML Forms is added so as to completely discard need for query string handling in Path, by requiring such query strings to be sent through POST body.
        2. This also means GET with query string has an alternative representation in QuasiHttp.
    3. User Headers: map of strings to list of strings, but with additional map of strings to strings interface.
    4. Body: object. However QPC Client API requires it to be IMessageSource (internally based on async FileStream or byte buffer wrapper)
        1. If using with memory-based transports, object doesn't have to be IMessageSource, but must be serializable.
        2. It is intended that this prop be replaceable by custom request processors.

7. QuasiHttpResponseMessage structure
    1. status-indicates-success: bool.
    1. status-indicates-client-error: bool. false means error is from server if status-indicates-success is false too (should be false if status-indicates-success is true).
    2. status-message
    3. content-length
    4. content-location (Internal).
    4. content-type
    2. User Headers
    5. Body

9. QpcClient API (works for server too, similar to how UdpClient works both ways).
    1. BeginPost(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void
    1. (can later add helper methods or helper class which only upload and download bodies, and automatically serializes bodies given enough serialization info)
    1. BeginProcessPost(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void
    2. BeginReceivePdu(data, offset, length): void
    2. BeginReset(Action<Exception> cb)
    3. Timeout prop
    4. EventLoop prop
    6. IQpcTransport prop
    5. IApplicationCallback prop for processing incoming requests.
    6. prop for temp file system - for creating and destroying files.
    7. prop for random id generator - used in names of files together with a timestamp.

10. Supporting types:
    1. QuasiHttpException. thrown if IsSuccess is false.
    if this error occurs, it will have a reference to the quasi http response message.

11. URL Path validation middleware (based on https://datatracker.ietf.org/doc/html/rfc1630). 
    1. Valid path characters aside forward slash and percent encoding %xx (ISO-8859-1): A–Z a–z 0–9 . - _ ~ ! $ & ' ( ) * + , ; = : @

12. Html Form validation middleware (based on https://url.spec.whatwg.org/#application/x-www-form-urlencoded).
    1. Special characters which must be encoded (percent encoding in utf-8): = & + % ('+' means space).

13. Header validation middleware
    1. key or value cannot contain newlines
    2. case sensitive keys only
    3. keys starts with English alphabet, and can contain other English alphabets, hyphens or decimal digits.
    4. values: only printable ASCII.
