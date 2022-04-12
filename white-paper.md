# Introduction

1. Kabomu Library is a message passing framework for building networking protocols for distributed systems.
2. The term "message" as used in Kabomu denotes a stream of bytes which is usually assumed to be bounded. Kabomu seeks to support frameworks which already implement support for exchanging bounded messages, by implementing support for exchanging unbounded messages as well.
3. All protocols based on the framework support both explicitly acked one-way request (for the purpose of data transfer), and implicitly acked request-response message exchange patterns.
4. Some protocols work with immediate processing while others work with deferred processing.
5. Protocols which employ immediate processing are "semantically compatible" with HTTP/1.1, so that terminology of request methods, response status codes, headers, and chunked request and response bodies apply. They differ only in their underlying transports, which are not limited to TCP.
   1. As such such protocols are said to belong to "Quasi-HTTP" protocol family.
   2. They require all headers from clients to start with "X-", and reserves all headers without "X-" prefix.
6. Protocols which employ deferred processing are "semantically compatible" with a subset of JMAP Mail. They differ only in their underlying transports, which are limited to HTTP. Deferred processing will be done by automated email thread processors. 
7. Since webmail makes it possible to send email via HTTP, point 6 can still be seen from the alternative perspective of HTTP. This will require use of "dictionary of callbacks" idea for simulating deferred processing as immediate processing (callbacks are invoked with error of "not done yet" after some timeout to signal that request is really being processed in a deferred manner).
8. From playing around with RINA research, my opinion is that it is not useful to have long lived connections or flows which have to tolerate idleness or even restarts of peers. Rather I propose that it is enough to have unbounded message exchanges which neither tolerate idleness nor restarts of peers, and scale better horizontally. Thus I had to drop RINA's concepts, and instead continue to emulate success of HTTP's stateless request-response model.

## Protocol Requirements

These are the challenges which all protocols of Kabomu must address in order to exchange messages by sending requests and receive replies (including empty replies).

It must be noted that the using protocols resemble making local procedure calls. However when communication goes beyond a single host (computer), certain challenges become possible which are absent in making local procedure calls.

1. A message may be lost or destroyed in the network (e.g. due to a network disconnection), and hence will never reach its intended destination.
2. A message may experience significant delays during its transmission, so that the usual style of blocking for responses during local procedure call may be too inefficient. This suggests that protocols use non-blocking style (e.g. through callbacks) for efficient utilization of computing resources.
3. Networks can hold on to messages for significantly long times, even after interested communicating parties have given up on them. This suggests that even if a communicating party is no longer interested in a transmission, a protocol may have to hold on to it for some time before abandoning it as well.
4. The network can outlive restarts of communicating parties. This means that transmissions before restarts can interfere with those created afterwards, and suggests that after startup, protocols must be sure all previous transmissions have ended before starting new ones.
3. A message may be duplicated in the network, so that interested communicating parties see it more than once. This combined with the previous two points (delays and outliving restarts) is the challenge of **duplicate protection**, which is the greatest challenge of all. *Providing duplicate protection meets almost all protocol requirements.*.
4. Upgrading a protocol doesn't automatically result in all communicating parties using the latest version of it. In general, communicating parties may use different versions of a protcol, and hence upgrades must be carefully done to be backward compatible.
4. A message may be larger than what a network is capable of transmitting at once, or larger than what the receiving end is capable of processing at once. This implies that some messages must be chopped into sizable pieces at the sender for transmission. Correspondingly, on delivery such chopped-up pieces must be combined into one for the receiver so that the receiver just gets a single message in the end.
5. Messages may accumulate in a receiver's buffers faster than the receiver can handle, when messages are being sent in batches. A mechanism known as flow control must be implemented to slow down senders.
3. *Duplicate protection is nearly infeasible without sequential message processing*. 
   1. This is an observation that took me almost 2 years (2020-2022) to discover, as I researched in vain for an alternative duplicate protection scheme other than TCP's solution. 
   2. However, sending messages via stop-and-wait (like is done during local procedure calls) is inefficient for networking across computers where the physical medium limits requests to around 1500 bytes, because one-at-a-time sending of such "small" messages underutilises the expensive network capacity.
   3. For proper utilisation of network capacity, these small messages must be sent in parallel. But that means they may arrive out of order at receiver. Hence flow control must be made more complex than stop-and-wait in order for use to send messages in parallel and still ensure correct sequence of message processing.
5. Messages may accumulate in a network's buffers faster than the network can handle. This is the problem of network congestion. The mechanism of flow control must be augmented to deal with this problem, in order to slow down senders to the satisfaction of not only receivers, but of the network as well.
5. A message may be modified accidentally or maliciously by the network before reaching its intended destination, so that destination gets a corrupted or wrong message. This suggests the need for some cross checks on received messages, or some more comprehensive security measures.

The deployment environments of Kabomu are the Internet via TCP/TLS, and within the same computing host via IPC mechanisms. What Kabomu seeks to do is to emulate some of HTTP and TCP's design to create few protocols in order to
   1. leverage the greater efficiency made possible by intrahost communications for request-response protocols involving unbounded messages, similar to HTTP. In other words, use of TCP on localhost for HTTP is not necessary when faster and more memory efficient alternatives exist.
   2. simplify network programming by increasing robustness of persistent TCP connections.

Concerning request-response protocols: Most HTTP libraries out there assume (rightly) that the underlying protocol is TCP, and so do not abstract away the underlying transport for possible use with IPC mechanisms.

Also it is my observation that for communications not involving Web browsers, HTTP itself is strictly needed only in its semantics and programming API, and not its syntax. Thus new protocols (aka Quasi-HTTP protocols) can be developed for implementing a subset of HTTP semantics, but with different syntax, especially with different underlying transports other than TCP.

Concerning simplicity of network programming: Actually TCP/TLS and IPC mechanisms already meet all the protocol requirements listed above. *It just happens however, that when trying to make TCP more robust by retrying connections, the more robust protocol needed cannot leverage the duplicate protection already provided by TCP, and instead has to solve that problem again.*

In the end, Kabomu has to develop new protocols (aka Quasi-TCP protocols) which serve as upper layers over TCP or any other protocol, whiles maintaining duplicate protection, regardless of whether it provides duplicate protection or not, and regardless of whether it acknowledges receipt of messages or not.

Fortunately Kabomu need not reinvent TCP all over again, because it can leverage other TCP solutions to the general protocol challenges to simplify the design of Quasi-TCP.

For example Quasi-TCP doesn't handle security and congestion control because TCP/TLS and IPC mechanisms already cater for that.

Next is how Quasi-TCP sidesteps a major complexity of TCP's design, which comes from usage of 4-byte connection identifiers consisting of source and destination ports; and sequence numbers which do not have a definite initial value. Several features of TCP such as time wait state, quiet time state, ISN selection and PAWS exist because of this.

In contrast Quasi-TCP uses 16-byte connection identifiers; and sequence number with a definite initial value, tremendously simplifying its design.

The most important benefit of TCP and IPC mechanisms for Quasi-TCP though, is their ability to transmit payloads far larger than the physical limit of 1500 imposed by Ethernet (or worse still 512 imposed by UDP).

This comes from an essential complexity of TCP's design, which is its windowing flow control which is used to efficiently utilize the physical networks. Due to the end result of this hard work by TCP, which is the ability to send large payloads, Quasi-TCP does not worry about the challenge of sending of small messages inefficiently utilizing physical networks, and hence settles for stop-and-wait flow control.

To prevent clients from having to deal with uncertainty about the safest minimum message size to use, Kabomu offers clients a minimum value of **30KB**. Beyond this limit, clients must be prepared to fragment and reassemble messages on their own.

## QPC-Connection

This QPC service provides duplicate protection by emulating TCP but with the use of uuids.

1. Need for duplicate protection shows up in many vital ways. Most prominently, it helps to retry any kind of request, regardless of whether it is idempotent or not, in order to increase robustness.
      1. This simplifies TCP programming because connections can be allowed to die due to inactivity, and replaced with new ones at any time.
      2. Also this enables improving efficiency of HTTP in a much simpler way compared to HTTP/2 and HTTP/3, by using TCP connection pools and multiplexing HTTP requests over a single connection.

1. A connection is identified by a uuid known both at a sending end of a network, and a corresponding receiving end.

1. Sender starts by asking receiver to allocate a uuid for it, using 4-byte request ids.

2. After this, requests can be sent, but must be sent and processed in sequence.

3. Anytime a request has to be made, a last received expected sequence id (or 0) is used, and then pdu types "req" and "resp" are used to process the request.

3. A "resp" pdu must always indicate the next expected sequence number.

4. After a response is generated, the receiver enters a "dally" state, waiting for the sender to retry in case the response is lost. Anytime the receiver gets a sequence number which is expected, it exits the "dally" state and discards processing of all previous sequence numbers.

4. Use pdu type "fin" to dispose off a connection at the remote receiving end. It is not required for the "fin" pdu to arrive successfully; the receiver will always dispose itself upon an idle timeout.

4. Connections cannot be used again upon any kind of failure. All failures are fatal. *In particular, connections cannot survive failures due to idleness in request processing or restarts of communicating peers.*

4. Sequence numbers start from 0 up to their maximum 4-byte unsigned value, and can wrap around. Internal duplication is prevented by the imposition of a minimum wrap around time (which equals the maximum packet lifetime of the network). This strategy works because this protocol starts at a definite value unlike TCP, and so it can be assumed that by the time of wrap around all sequence numbers have been used up.

6. Since a minimum wrap around time imposes a maximum transmission rate, then *there is the possibility of failure on a connection because the network is too fast.* For use with QPC-ConnectionPool below, an option can be added to skip the failure and just forcefully wait till  minimum wrap arount time elapses.

1. The constraints of not being able to process requests in parallel, survive idleness, survive restarts, and transmit without speed limits, can be removed when this protocol is used with QPC-ConnectionPool, which is described next.


## QPC-ConnectionPool

This QPC service complements QPC-Connection to provide duplicate protection without constraints of sequential request processing and maximum transmission rate.

1. This protocol treats QPC-Connection abstractly, and so can be used with connection-oriented protocols in general for the purpose of limiting the number of concurrent requests.

1. This it does by maintaining a list of connections (a.k.connection pool), through which requests can be sent over for processing.

2. So basically if a request is handed to a sender, the sender determines whether to utilize an existing connection or create a new one. Either way, request can be sent in parallel with other ongoing ones.

3. Retry is done at most once to simplify protocol, by leveraging a fact in [Amazon docs](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/) that usually a one-time retry resolves almost all transient connection errors.
    1. So when a request is first sent and the connnection being used returns a failure indicating that request was not processed, the request can be retried with a new connection.
    2. Else if it is unknown whether the request was processed or not, then the failure will be taken as fatal, and no retry will be performed.
