﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Class of errors thrown to indicate that a given key required to be present in a
    /// registry was not found.
    /// </summary>
    public class NotInRegistryException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public NotInRegistryException(string message) : base(message)
        {

        }
    }
}
