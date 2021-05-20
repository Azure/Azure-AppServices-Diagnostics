using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Exceptions
{
    public class ObserverException : Exception
    {
        public ObserverException(string url, string statusCode, string message, Exception innerException) : base($"Observer StatusCode: {statusCode}, URL: {url}, Message: {message}", innerException)
        {

        }
    }
}
