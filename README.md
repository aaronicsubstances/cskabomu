# Kabomu - C#.NET Core version

Kabomu is a quasi web server gateway interface, that enables building quasi web applications that can connect endpoints within localhost and even within an OS process, by employing IPC mechanisms other than TCP. 

From another standpoint, Kabomu is an inter-module communication library intended to help build more maintainable monolithic applications, by making it possile to enforce modular boundaries and software architecture.

## Mission

Overall mission is toward monolithic applications for enforcement of architecture and better preparation for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable input and output parameters, to prepare for transportation of messages across a network.
4. quasi web requests, to provide alternative request-response protocols resembling http, and also to ease transition to http usage
5. quasi web mail, for deferred processing thanks to automated email thread processors, and "dictionary of callbacks with ttl" idea for simulating deferred processing as immediate processing.

## Design

1. Deployment enviroment: localhost

1. Quasi Web transport type: connection-oriented, byte-oriented.

1. Quasi Web transports: memory, localhost TCP, unix domain socket, windows named pipe, HTTP.
   1. *Support for HTTP makes it possible to use Kabomu with any HTTP client library.*

3. Quasi Web Protocol: resembles HTTP/1.1 chunk encoding, in which request and response headers are sent in "lead" chunks.

3. Multithreading strategy: *asynchronous* mutual exclusion API of which NodeJS-style event loops and dear old locks are specific implementations.
   1. Also leverages atomic compare-and-swap CPU instructions to skip mutual exclusion entirely where possible.

3. Quasi Web request processing strategy: middleware-based; resembles [ExpressJS](https://expressjs.com/), [Ratpack](https://ratpack.io/)
   1. *Provision of a middleware-based request processing pipeline makes it possible to use Kabomu with any Web application framework*
