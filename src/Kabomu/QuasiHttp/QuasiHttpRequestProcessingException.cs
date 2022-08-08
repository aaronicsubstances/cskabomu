﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Error thrown by instances of <see cref="Client.StandardQuasiHttpClient"/> and
    /// <see cref="Server.StandardQuasiHttpServer"/> which can provide details of error conditions
    /// in the form of numeric reason codes.
    /// </summary>
    /// <remarks>
    /// The reason codes in 0-9 which currently do not have an assigned meaning are reserved for use by this class. 
    /// All other numbers including negative values may be used as reason codes.
    /// </remarks>
    public class QuasiHttpRequestProcessingException : QuasiHttpException
    {
        /// <summary>
        /// Indicates general error without much detail to offer aside inspecting error messages and inner exceptions.
        /// </summary>
        public const int ReasonCodeGeneral = 1;

        /// <summary>
        /// Indicates a timeout in processing.
        /// </summary>
        public const int ReasonCodeTimeout = 2;

        /// <summary>
        /// Indicates that request processing has been explicitly cancelled by an end user.
        /// </summary>
        public const int ReasonCodeCancelled = 3;

        /// <summary>
        /// Indicates that request processing has been cancelled as part of a reset operation
        /// on <see cref="Client.StandardQuasiHttpClient"/> and <see cref="Server.StandardQuasiHttpServer"/>
        /// instances.
        /// </summary>
        public const int ReasonCodeReset = 4;

        // the following codes are reserved for future use.
        private const int ReasonCodeReserved5 = 5;
        private const int ReasonCodeReserved6 = 6;
        private const int ReasonCodeReserved7 = 7;
        private const int ReasonCodeReserved8 = 8;
        private const int ReasonCodeReserved9 = 9;
        private const int ReasonCodeReserved0 = 0;

        /// <summary>
        /// Creates a new instance an error message and with reson code <see cref="ReasonCodeGeneral"/>.
        /// </summary>
        /// <param name="message">the error message.</param>
        public QuasiHttpRequestProcessingException(string message) :
            this(ReasonCodeGeneral, message, null)
        {
        }

        /// <summary>
        /// Creates a new instance with an error message and a reason code.
        /// </summary>
        /// <param name="reasonCode">reason code to use</param>
        /// <param name="message">error message</param>
        /// <exception cref="ArgumentException">The <paramref name="reasonCode"/> argument is reserved for future use
        /// by this class</exception>
        public QuasiHttpRequestProcessingException(int reasonCode, string message) : 
            this(reasonCode, message, null)
        {
        }

        /// <summary>
        /// Creates a new instance with an error message, a reason code and
        /// a reference to the inner exception that is the cause of this exception..
        /// </summary>
        /// <param name="reasonCode">reason code to use</param>
        /// <param name="message">the error message</param>
        /// <param name="innerException">cause of this exception</param>
        /// <exception cref="ArgumentException">The <paramref name="reasonCode"/> argument is reserved for future use
        /// by this class</exception>
        public QuasiHttpRequestProcessingException(int reasonCode, string message, Exception innerException) :
            base(message, innerException)
        {
            switch (reasonCode)
            {
                case ReasonCodeReserved5:
                case ReasonCodeReserved6:
                case ReasonCodeReserved7:
                case ReasonCodeReserved8:
                case ReasonCodeReserved9:
                case ReasonCodeReserved0:
                    throw new ArgumentException("cannot use reserved reason code: " + reasonCode,
                        nameof(reasonCode));
                default:
                    break;
            }
            ReasonCode = reasonCode;
        }

        public int ReasonCode { get; }
    }
}