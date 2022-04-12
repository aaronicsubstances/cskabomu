# C#.NET Core Support for Kabomu Library

## Mission

Overall mission is toward monolithic applications for enforcement of architecture and better preparation for evolution to distributed systems, through

1. extending powers of local procedure call via shared memory, to quasi procedure calls (potentially involving a network) via message passing
2. support for input and output stream parameters, to prepare for all kinds of message passing
3. serializable input and output parameters, to prepare for transportation of messages across a network. Note:
    1. in-memory qpc facility will only serialize with a small but non-zero probability, and rather use fallback payloads to improve performance.
    2. Deserialization must always be prepared for absence of fallback payloads.
4. quasi web requests, to provide alternative request-response protocols resembling http, and also to ease transition to http usage
5. quasi web mail, for deferred processing
