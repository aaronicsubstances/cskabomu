﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QpcPdu
    {
        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int RequestId { get; set; }
        public int EmbeddedQuasiHttpBodyLength { get; set; }
        public QuasiHttpRequestMessage WrappedRequest { get; set; }
        public QuasiHttpResponseMessage WrappedResponse { get; set; }
        public string Origin { get; set; }
        public string Host { get; set; }
        public string Verb { get; set; }
        public string BodyLocation { get; set; }
        public byte[] EmbeddedBody { get; set; }
        public int EmbeddedBodyOffset { get; set; }
        public int EmbeddedBodyLength { get; set; }
    }
}
