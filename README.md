# Kabomu - C#.NET Core version

Kabomu is a networking library that extends the semantics of HTTP to transports other than TCP on localhost. It enables building applications running on non-IP networks which will resemble web applications.

Such applications are termed "quasi web applications" in Kabomu. The end result is that Kabomu enables building quasi web applications that can connect endpoints within localhost and even within an OS process, through IPC mechanisms other than TCP.

## Purpose

Kabomu seeks to demonstrate the following:

1. *Quasi procedure call framework*, i.e. offers improvement over RPC frameworks such as Protocol Buffers and Apache Thrift, by offering stream input parameters, stream return/output values, flexible timeout specification, non-TCP transports and a transition path to HTTP.

2. *Quasi web protocol*, i.e. offers another request-response protocol which exactly resembles HTTP/1.1 in its semantics, such that it can be executed in memory, executed with IPC mechanisms, or executed as actual HTTP.

3. *Cross-platform quasi web framework*, i.e. offers a web framework pattern which can be shared across programming languages, and does not assume the use of actual HTTP.


## Design

1. Deployment enviroment: mainly localhost, but can be extended to the Internet via HTTP.

1. Quasi web transports demonstrated: memory, localhost TCP, unix domain socket, windows named pipe, HTTP.
   1. *Support for HTTP makes it possible to use Kabomu with any HTTP client library.*
   2. Interfaces are provided for the creation of any custom quasi web transport.

3. Quasi web protocol: resembles HTTP/1.1 chunk encoding, in which request and response headers are sent in "lead" chunks.

3. Quasi web request processing strategies: one of the following
   1. use Kabomu.Mediator: it is a quasi web framework that was inspired by [ExpressJS](https://expressjs.com/) and [Ratpack](https://ratpack.io/) and meant to be implemented in multiple programming languages. It is called "quasi web framework" because it does not assume the use of TCP.
   2. use an existing web server gateway interface (e.g. Python WSGI, C#.NET OWIN, Java Servlet, Ruby Rack) and hook it to a quasi web transport. Existing web frameworks can then be used as usual.
   3. use data transfer objects to separate concerns between services and access points:
       1. Assume that the services to be developed with favourite web framework can be accesssed by different web framework or networking protocol aside HTTP, with different security policies and serialization mechanisms.
       2. Then design the services independently of favourite web framework by using data transfer objects based on types provided by the Kabomu.Mediator framework, and custom types as needed.
       3. It should then be easier to switch networks without impacting services implementing business logic.


## Usage

See [Examples](https://github.com/aaronicsubstances/cskabomu/tree/main/examples) folder for sample file serving programs based on each default quasi web transport.

The sample programs come in pairs (with the exception of the memory-based one):  a client program and corresponding server program. The server program must be started first. By default a client program uploads all files from its current directory to a folder created in the server program's current directory.

The Program.cs source file of each sample program indicates how to change the default client and server endpoints (TCP ports or paths) with command line arguments. The directories of upload and saving can also be changed with command line arguments.
