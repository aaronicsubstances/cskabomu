# Kabomu - C#.NET Core version

Kabomu is a software communication library that seeks to extend the success story of HTTP to protocol stacks other than TCP/IP. It enables building applications running on non-IP networks which will resemble web applications.

Such applications are termed "quasi web applications" in Kabomu. The end result is that Kabomu enables building quasi web applications that can connect endpoints within localhost and even within an OS process, through IPC mechanisms other than TCP.

## Mission

Overall mission starts with enforcing the architecture of monolithic applications and better preparing them for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable input and output parameters, to prepare for transportation of messages across a network.
4. quasi web requests, to provide alternative request-response protocols resembling http, and also to ease transition to http usage
5. quasi web mail, for deferred processing thanks to log-based message brokers, and memory cache of callbacks with sliding expiration for simulating deferred processing as immediate processing.

## Design

1. Deployment enviroment: mainly localhost, and then the Internet via HTTP.

1. Quasi web transports provided by default: memory, localhost TCP, unix domain socket, windows named pipe, HTTP.
   1. *Support for HTTP makes it possible to use Kabomu with any HTTP client library.*
   2. Interfaces are provided for the creation of any custom quasi web transport.

3. Quasi web protocol: resembles HTTP/1.1 chunk encoding, in which request and response headers are sent in "lead" chunks.

3. Multithreading strategy: *asynchronous* mutual exclusion API of which NodeJS-style event loops and dear old locks are specific implementations.
   1. Also leverages atomic compare-and-swap CPU instructions to skip mutual exclusion entirely where possible.

3. Quasi web request processing strategies: one of the following
   1. use upcoming Kabomu.WebFramework: it resembles [ExpressJS](https://expressjs.com/), [Ratpack](https://ratpack.io/)
   1. use an existing web server gateway interface (e.g. Python WSGI, C#.NET OWIN, Java Servlet, Ruby Rack) and hook it to a quasi web transport.
   2. separation of concerns between services and access points:
       1. Assume that the services to be developed over favourite web framework can be accesssed by different web framework or networking protocol aside HTTP, with different security policies and serialization mechanisms.    
       2. Then design the services independently of favourite web framework (e.g. services should not employ any classes of favourite web framework).
       3. It should then be easier to switch networks without impacting services implementing business logic.


## Usage

See [Examples](https://github.com/aaronicsubstances/cskabomu/tree/main/examples) folder for sample file serving programs based on each default quasi web transport.

The sample programs come in pairs (with the exception of the memory-based one):  a client program and corresponding server program. The server program must be started first. By default a client program uploads all files from the its current directory to a folder created in the server program's current directory.  

The Program.cs source file of each sample program indicates how to change the default client and server endpoints (TCP ports or paths) with command line arguments. The directories of upload and saving can also be changed with command line arguments.
