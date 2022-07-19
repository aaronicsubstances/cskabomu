using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents a single contigious range of bytes in a byte array.
    /// </summary>
    /// <remarks>
    /// This class is intended to help clients formulate a wider strategy of reducing or eliminating byte array copying in 
    /// networking protocols, and also during serialization to byte arrays.
    /// <para>
    /// Usually the data which a current layer has to pass to a next lower layer is the data received by the current layer
    /// prepended and/or appended with extra information determined by the current layer.
    /// Also the lower layer usually expects that data to be in the form of an opaque byte array for the sake of separation of 
    /// concerns. But then that may force the current layer to create another byte array large enough to hold the additional information,
    /// and then copy the data it received into this array. This can lead to a lot of copying across layers of a network protocol.
    /// </para>
    /// <para>
    /// With this class one can instead create two instances for only the
    /// extra information at the beginning and end of the received data of a protocol layer. The received data itself can be
    /// wrapped in a third instance. Next lower layer can then receive an array of these three instances for processing.
    /// </para>
    /// </remarks>
    public class ByteBufferSlice
    {
        /// <summary>
        /// Gets or sets backing byte array.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the offset in the backing byte array.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the count of contigious bytes in the backing byte array
        /// starting from the offset.
        /// </summary>
        public int Length { get; set; }
    }
}
