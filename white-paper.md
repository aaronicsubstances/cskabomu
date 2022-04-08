# Introduction

1. Kabomu Library is a message passing framework for building networking protocols for distributed systems.
2. A message is just a stream of bytes which is usually assumed to be bounded. Kabomu seeks to support frameworks which already support for exchanging bounded messages, by implementing support for exchanging unbounded messages as well.
3. All protocols based on the framework support both explicitly acked one-way request (for the purpose of data transfer), and implicitly acked request-response message exchange patterns.
4. Some protocols work with immediate processing while others work with deferred processing.
5. Protocols which employ immediate processing are "semantically compatible" with HTTP/1.1, so that terminology of request methods, response status codes, headers, and chunked request and response bodies apply. They differ only in their underlying transports, which are not limited to TCP.
   1. As such such protocols are said to belong to "Quasi-HTTP" family.
   2. They require all headers from clients to be sent to start with "X-", and reserves all headers without "X-" prefix.
   3. Known reserved headers are: "Method", "Path", "Status", and "Content-Length".
6. Protocols which employ deferred processing are "semantically compatible" with a subset of JMAP Mail. They differ only in their underlying transports, which are limited to HTTP. Deferred processing will be done by automated email thread processors. 
7. Since webmail makes it possible to send email via HTTP, point 6 can still be seen from the alternative perspective of HTTP. This will require use of "dictionary of callbacks" idea for simulating deferred processing as immediate processing (callbacks are invoked with error of "not done yet" after some timeout to signal that request is really being processed in a deferred manner).
8. From playing around with RINA research, my opinion is that it is not useful to have long lived connections or flows which have to tolerate idleness or even restarts of peers. Rather I propose that it is enough to have unbounded message exchanges which neither tolerate idleness nor restarts of peers, and scale better horizontally. Thus I had to drop RINA's concepts, and instead continue to emulate success of HTTP's stateless request-response model.

# Quasi Procedure Call (QPC) Services for Kabomu

## Requirements

These are the challenges which all QPC services of Kabomu must address in order to send requests and receive replies similar to making local procedure calls. This challenges arise because communication/transportation can go beyond a single host (computer). Hence they are not present in making local procedure calls.

1. A request or response may be lost or destroyed in the network (e.g. due to a network disconnection), and hence will never reach its intended destination.
2. A request or response may experience significant delays during its transmission, so that the usual style of blocking for responses during local procedure call may be too costly. This suggests that QPC services 
    1. use non-blocking style (e.g. through callbacks) to lower cost of such delays on computing resources of the sender.
    2. optionally support cancellation.
3. Networks can hold on to requests for significantly long times, even after sender has given up on it. This suggests that even if a sender is no longer interested in a transmission, a QPC service may have to hold on to it for some time before abandoning it as well.
4. The network can outlive restarts of senders and receivers. This means that transmissions before restarts can interfere with those created afterwards, and suggests that a QPC service may have to slow down after restart,  before processing requests at full capacity.
3. A request or response may be duplicated in the network, so that the intended destination sees it more than once. This combined with the previous two points (delays and outliving restarts) is the challenge of **duplicate protection**, which is the greatest challenge of all. *Providing duplicate protection solves almost all of the challenges of quasi procedure calls.* In fact, QPC services of Kabomu differ mostly in their solution to this problem.
3. *Duplicate protection is nearly infeasible without sequential request processing*. 
   1. This is an observation that took me almost 2 years to discover, as I researched in vain for an alternative duplicate protection scheme other than TCP's solution. 
   2. However, sending requests one by one during local procedure calls is inefficient for networking across computers, because it underutilises the expensive network capacity. 
   3. For proper utilisation of network capacity, requests must be sent in batches. Since each request in a batch may be delayed differently from others (or worse dropped), they may arrive out of order, and hence a complex mechanism is required to ensure correct sequence of request processing.
4. Upgrading QPC service doesn't automatically result in both ends of communication using it. It is possible that only one end uses the latest version, and hence upgrades must be carefully done to be backward compatible.
4. A request or response may be larger than what a network is capable of transmitting at once, or larger than what the receiving end is capable of processing at once. This implies that some requests must be chopped into sizable pieces at the sender for transmission. Correspondingly, on delivery such chopped-up pieces must be combined into one for the receiver so that the receiver just gets a single message in the end.
5. Requests may fill up a receiver's buffers faster than the receiver can handle, when requests are being sent in batches. A mechanism known as flow control must be implemented to slow down senders.
5. Requests and responses may fill up network's buffers faster than network can handle. This is the problem of network congestion. The mechanism of flow control must be augmented to deal with this problem, in order to slow down senders to the satisfaction of not only receivers, but of the network as well.
5. A request or response may be modified accidentally or maliciously by the network before reaching its intended destination, so that destination gets a corrupted or wrong message. This suggests the need for some cross checks on received messages, or some more comprehensive security measures.

The deployment environments of Kabomu are the Internet via TCP/TLS and within the same computing host via IPC mechanisms. What Kabomu seeks to do is to emulate some of TCP's design to create few protocols which support lower layers other than IP, for the sake of
   1. leveraging the greater efficiency made possible by intrahost communications. In other words, use of TCP on localhost is not necessary when faster and more memory efficient alternatives exist (e.g. by dispensing with slow starts and time wait states).
   2. providing extra reliability layers over TCP.

Actually TCP/TLS and almost all the IPC mechanisms address all of the QPC challenges directly. Kabomu's design goal is to leverage their convenient features for simplicity in its protocols. For example Kabomu doesn't handle security and congestion control because TCP/TLS and IPC mechanisms already cater for that.

Kabomu also leverages TCP windowing flow control to great effect through multiplexing, so that it is enouogh to use the simplest flow control method which is stop-and-wait.

The most important benefit of TCP and IPC mechanisms for Kabomu is their ability to transmit large payloads (ie far larger than 512 of DNS via UDP or 1500 of Ethernet).

To prevent clients from having to deal with uncertainty about the safest minimum payload size to use, Kabomu offers clients a minimum value of **30KB**. Thus all underlying QPC mechanisms which may be used by Kabomu QPC services, *can send messages of up to 30KB in size*. 30KB is enough to carry typical requests and responses of quasi procedure calls. Beyond this limit, clients must be prepared to fragment and reassemble payloads on their own.

The QPC services for Kabomu leverage this benefit of large payloads, to employ 25 bytes worth of transmission overhead, since such overhead will be a small fraction of payloads. A major complexity of TCP's design comes from usage of sequence numbers. Several features of TCP such as time wait state, quiet time state, ISN selection and PAWS exist because of this. Kabomu uses 16-byte uuids in addition to sequence numbers (but without wrap around), and is thus able to sidestep all of the complexity involved in relying only on sequence numbers, but at the cost of slightly larger payloads.

## QPC-Connection

This QPC service emulates TCP but uses uuids in addition to sequence numbers for duplicate protection.

1. A uuid uniquely identifies a pair of instances at each end of a network: one a sending end, and the other a receiving end.

1. Sender starts by asking receiver to allocate a uuid for it, using 4-byte request ids.

2. After this, requests can be processed in sequence.

3. Anytime a request has to be made, a new sequence id is generated, and then pdu types "req" and "resp" are used to process the request.

3. A "resp" must always indicate the next acceptable sequence number.

4. Sequence numbers are not allowed to wrap around. Instead the instances being used have to be disposed.

4. Use pdu type "fin" to dispose off an instance at the remote receiving end. It is not required for the "fin" pdu to arrive successfully; the receiver will always dispose itself upon an idle timeout.

4. Instances cannot be used again upon any kind of failure. All failures are fatal. *In particular, instances cannot survive failures due to idleness in request processing or restarts of communicating peers.*


## QPC-ConnectionPool

This QPC service utilises QPC-Connection to eliminate constraint of sequential request processing, while maintaining duplicate protection.

1. This it does by maintaining a list of QPC-Connection instances, through which requests can be sent over for processing.

2. So basically if a request is handed to a sender, the sender determines whether to utilize an existing QPC-Connection or create a new one. Either way, request can be sent in parallel with other ongoing ones.

3. To prevent edge case of duplicate processing, when a request is sent *once*, and the instance being used returns a failure indicating non-existence of a uuid, the request can be retried with a new instance (unless it is already known that a new instance was created to start with, in which case failure is fatal).

4. But if a request is retried *at least once*, then it should not be possible to create another instance; any failure must be fatal.
