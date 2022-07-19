using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Declares an error handling routine which clients can use to pass on 
    /// errors to logging frameworks.
    /// </summary>
    /// <param name="error">any error thrown</param>
    /// <param name="message">any error message provided</param>
    public delegate void UncaughtErrorCallback(Exception error, string message);
}
