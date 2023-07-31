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
        /// Commits this instance by sending out the underlying quasi http response An error
        /// is thrown if instance has already been committed.
        /// </summary>
        /// <exception cref="ResponseCommittedException">The instance has already been committed.</exception>
        void Send();

        /// <summary>
        /// Commits this instance by sending out the underlying quasi http response
        /// but only if this instance has not already been committed.
        /// </summary>
        /// <returns>true if this is the first time an attempt to commit is being made;
        /// false if this instance has already been committed.</returns>
        bool TrySend();

        /// <summary>
        /// If instance has not been committed, then this method makes some changes to this instance before committing it
        /// by sending out the underlying quasi http response, and returning true. However if this
        /// instance has already been committed, no changes are made and false is returned.
        /// </summary>
        /// <param name="changesCb">callback which will be invoked to make changes before committing. will not
        /// be invoked if instance has already being committed. Can be null</param>
        /// <returns>true if this is the first time an attempt to commit is being made;
        /// false if this instance has already been committed.</returns>
        bool TrySend(Action changesCb);
    }
}
