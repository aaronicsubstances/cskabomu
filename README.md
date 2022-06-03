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

1. Quasi Web transport wrapper: connection-oriented, byte-oriented, message-oriented (ie datagram).

1. Quasi Web transports: memory, TCP, unix domain socket, windows named pipe.

3. Multithreading strategy: event loop

3. Web request processing strategy: ExpressJS

3. Protocol: mimicks Sun RPC, HTTP/1.1 and HTTP/2.

3. Protocol syntax: binary preamble, CSV headers, and binary trailer
    1. preamble is concatenation of version, pduType, requestId, flags, embeddedHttpBodyLen.
    2. CSV is used for http headers and HTML forms. Each CSV row is a key followed by multiple values. NB: keys are case-sensitive.
    3. the following headers are reserved for use by the protocol, and hence will be ignored or lead to errors if set by clients: content-length, transfer-encoding (fixed to "chunked"), trailer, te, upgrade, connection, keep-alive, proxy-authenticate, proxy-authorization, accept-encoding (fixed to "identity"), content-encoding.
    4. See also https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3

5. IApplicationCallback interface
    1. BeginProcessPost(QuasiHttpRequestMessage, Action<Exception, HttpResponseMessage>)

5. QpcTransport API
    1. BeginSendPdu(data, offset, length, Action<Exception> cb): void
    1. ShouldProcessPost property - for processing outgoing requests in transports capable of doing so
    2. ProcessPost(QuasiHttpRequestMessage, Action<HttpResponseMessage>)

6. QuasiHttpRequestMessage structure
    1. path
    4. content-length: int. can be negative to indicate unknown size.
    4. content-type: one of application/octet-stream, application/json (always UTF-8), text/plain (always UTF-8), application/x-www-form-urlencoded (always UTF-8).
        1. Body type for HTML Forms is added so as to completely discard need for query string handling in Path, by requiring such query strings to be sent through POST body.
        2. This also means GET with query string has an alternative representation in QuasiHttp.
    3. headers: map of strings to list of strings
    4. body: IMessageSource.
        1. If using with memory-based transports, IMessageSource implementations can cleverly skip serializable to maintain speed of call-by-reference communications.

7. QuasiHttpResponseMessage structure
    1. status-indicates-success: bool.
    1. status-indicates-client-error: bool. false means error is from server if status-indicates-success is false too (should be false if status-indicates-success is true).
    2. status-message
    3. content-length
    4. content-type
    2. headers
    5. body

9. QpcClient API (works for server too, similar to how C#.NET's UdpClient class works both ways).
    1. BeginPost(QuasiHttpRequestMessage, timeoutOptions, Action<Exception, QuasiHttpResponseMessage> cb): void
    1. (can later add helper methods or helper class which only upload and download bodies, and automatically serializes bodies given enough serialization info)
    1. BeginProcessPost(QuasiHttpRequestMessage, Action<Exception, QuasiHttpResponseMessage> cb): void
    2. BeginReceivePdu(data, offset, length): void
    2. BeginReset(Action<Exception> cb)
    3. Default Timeout prop
    4. EventLoop prop
    6. IQpcTransport prop
    5. IApplicationCallback prop for processing incoming requests.

10. Supporting types:
    1. QuasiHttpException. thrown if IsSuccess is false.
    if this error occurs, it will have a reference to the quasi http response message.

11. URL Path validation middleware (based on https://datatracker.ietf.org/doc/html/rfc1630). 
    1. Valid path characters aside forward slash and percent encoding %xx (ISO-8859-1): A–Z a–z 0–9 . - _ ~ ! $ & ' ( ) * + , ; = : @

13. Header validation middleware
    1. key or value cannot contain newlines
    2. case sensitive keys only
    3. keys starts with English alphabet, and can contain other English alphabets, hyphens or decimal digits.
    4. values: only printable ASCII.
