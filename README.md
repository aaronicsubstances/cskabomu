# Kabomu - C#.NET Core version

Kabomu is a networking library that extends the semantics of HTTP to localhost via IPC mechanisms. It enables building applications running on unix domain sockets and
windows named pipes which can easily be upgraded to web applications.

Such applications are termed "quasi web applications" in Kabomu. The end result is that Kabomu enables building quasi web applications that can connect endpoints within localhost through IPC mechanisms other than TCP.

## Purpose

Kabomu seeks to demonstrate the following:

1. *Quasi procedure call framework*, i.e. offers improvement over RPC frameworks such as Protocol Buffers and Apache Thrift, by offering stream input parameters, stream return/output values, flexible timeout specification, non-TCP transports and a transition path to HTTP.

2. *Quasi web protocol*, i.e. offers another request-response protocol which exactly resembles HTTP/1.1 in its semantics and can be executed with IPC mechanisms.

4. *Abstraction of message queues*, by modelling them as fire and forget requests to email-like address groups (e.g. kafka topic, network multicast address).


## Design

1. Deployment enviroment: localhost.

1. IPC mechanisms demonstated: localhost TCP, unix domain sockets and windows named pipes
   2. Interfaces are provided to wrap around any other IPC

3. Quasi web protocol: resembles HTTP/1.1 chunk encoding, in which request and response headers are sent in "lead" chunks.

3. Quasi web request processing strategies: one of the following
   1. Ignore http request methods and response status messages, and also ignore request path parameters and query strings. Instead use entire request target/path as a key into a
   dictionary whose values are procedures which take a request and produce a response.
   2. use an existing web server gateway interface (e.g. Python WSGI, C#.NET OWIN, Java Servlet, Ruby Rack, NodeJS Connect) and hook it to an IPC mechanism. Existing web frameworks can then be used as usual.

## Usage

The entry classes of the libary are [StandardQuasiHttpClient](https://github.com/aaronicsubstances/cskabomu/tree/main/src/Kabomu/StandardQuasiHttpClient.cs), [StandardQuasiHttpServer](https://github.com/aaronicsubstances/cskabomu/tree/main/src/Kabomu/StandardQuasiHttpServer.cs).

See [Examples](https://github.com/aaronicsubstances/cskabomu/tree/main/examples) folder for sample file serving programs based on each example IPC.

The sample programs come in pairs:  a client program and corresponding server program. The server program must be started first. By default a client program uploads all files from its current directory to a folder created in the server program's current directory.

The Program.cs source file of each sample program indicates how to change the default client and server endpoints (TCP ports or paths) with command line arguments. The directories of upload and saving can also be changed with command line arguments.
