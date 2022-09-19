using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Wrapper for a quasi http response with methods to send the response out of a quasi http application
    /// represented by an instance of <see cref="MediatorQuasiWebApplication"/> class.
    /// </summary>
    public interface IContextResponse
    {
        /// <summary>
        /// Gets the underlying quasi http response.
        /// </summary>
        IQuasiHttpMutableResponse RawResponse { get; }

        /// <summary>
        /// Gets the status code of the <see cref="RawResponse"/> property.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Determines whether the status code of the <see cref="RawResponse"/> property lies within the
        /// range of 200-299 inclusive.
        /// </summary>
        bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Determines whether the status code of the <see cref="RawResponse"/> property lies within the
        /// range of 400-499 inclusive.
        /// </summary>
        bool IsClientErrorStatusCode { get; }

        /// <summary>
        /// Determines whether the status code of the <see cref="RawResponse"/> property lies within the
        /// range of 500-599 inclusive.
        /// </summary>
        bool IsServerErrorStatusCode { get; }

        /// <summary>
        /// Gets the body of the <see cref="RawResponse"/> property.
        /// </summary>
        IQuasiHttpBody Body { get; }

        /// <summary>
        /// Gets a wrapper for managing the headers in the <see cref="RawResponse"/> property.
        /// </summary>
        IMutableHeadersWrapper Headers { get; }

        /// <summary>
        /// Sets the status code of the <see cref="RawResponse"/> property to 200.
        /// </summary>
        /// <returns>the instance on this method was called, to enable chaining of other operations</returns>
        IContextResponse SetSuccessStatusCode();

        /// <summary>
        /// Sets the status code of the <see cref="RawResponse"/> property to 400.
        /// </summary>
        /// <returns>the instance on this method was called, to enable chaining of other operations</returns>
        IContextResponse SetClientErrorStatusCode();

        /// <summary>
        /// Sets the status code of the <see cref="RawResponse"/> property to 500.
        /// </summary>
        /// <returns>the instance on this method was called, to enable chaining of other operations</returns>
        IContextResponse SetServerErrorStatusCode();

        /// <summary>
        /// Sets the status code of the <see cref="RawResponse"/> property to a given value.
        /// </summary>
        /// <param name="value">the new status code value</param>
        /// <returns>the instance on this method was called, to enable chaining of other operations</returns>
        IContextResponse SetStatusCode(int value);

        /// <summary>
        /// Sets the body of the <see cref="RawResponse"/> property to a given value.
        /// </summary>
        /// <param name="value">the new quasi http response body</param>
        /// <returns>the instance on this method was called, to enable chaining of other operations</returns>
        IContextResponse SetBody(IQuasiHttpBody value);

        /// <summary>
        /// Commits this instance by sending the underlying quasi http response as the asynchronous result of
        /// a call to the <see cref="MediatorQuasiWebApplication.ProcessRequest"/> method. An error
        /// is thrown if instance has already been committed.
        /// </summary>
        /// <exception cref="ResponseCommittedException">The instance has already been committed.</exception>
        void Send();

        /// <summary>
        /// Commits this instance by sending the underlying quasi http response as the asynchronous result of
        /// a call to the <see cref="MediatorQuasiWebApplication.ProcessRequest"/> method, after changing its
        /// body to a given value. If instance has already been committed, its body remains unchanged, and
        /// an exception is thrown.
        /// </summary>
        /// <param name="value">the new quasi http response body</param>
        /// <exception cref="ResponseCommittedException">The instance has already been committed.</exception>
        void SendWithBody(IQuasiHttpBody value);

        /// <summary>
        /// Commits this instance by sending the underlying quasi http response as the asynchronous result of
        /// a call to the <see cref="MediatorQuasiWebApplication.ProcessRequest"/> method, but only if
        /// this instance has not already been committed.
        /// </summary>
        /// <returns>true if this is the first time an attempt to commit is being made;
        /// false if this instance has already been committed.</returns>
        bool TrySend();

        /// <summary>
        /// Commits this instance by sending the underlying quasi http response as the asynchronous result of
        /// a call to the <see cref="MediatorQuasiWebApplication.ProcessRequest"/> method, but only if this instance
        /// has not already been committed. And if instance has not been committed, its body is changed to a given
        /// value before it is committed.
        /// </summary>
        /// <param name="value">the new quasi http response body</param>
        /// <returns>true if this is the first time an attempt to commit is being made;
        /// false if this instance has already been committed.</returns>
        bool TrySendWithBody(IQuasiHttpBody value);
    }
}
