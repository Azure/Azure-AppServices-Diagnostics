using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Exceptions
{
    public class CustomerConsentException : Exception
    {
        public CustomerConsentException(string dataProviderName) : base($"Customer has not provided consent for {(dataProviderName != null ? dataProviderName : "this data")}")
        {
        }
    }
}
