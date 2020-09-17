using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Exceptions
{
    public class ApiNotSupportedException : NotSupportedException
    {
        private static string GENERIC_ERROR_MESSAGE = "The API: {0} is no longer supported. For further details, see updates sent to our distribution lists in regards to this API.";
        private static string GENERIC_ERROR_MESSAGE_WITH_SOLUTION = "The API: {0} is no longer supported. For further details, see updates sent to our distribution lists in regards to this API. Solution: {1}";

        public ApiNotSupportedException(string unsupportedApiName) : 
            base(string.Format(GENERIC_ERROR_MESSAGE, unsupportedApiName))
        {

        }

        public ApiNotSupportedException(string unsupportedApiName, string additionalDetails) : 
            base(string.Format(GENERIC_ERROR_MESSAGE, unsupportedApiName) + Environment.NewLine + "Details: " + additionalDetails)
        {

        }

        public ApiNotSupportedException(string unsupportedApiName, string solution, string additionalDetails = null) : 
            base(string.Format(GENERIC_ERROR_MESSAGE_WITH_SOLUTION, unsupportedApiName, solution) + (additionalDetails != null ? Environment.NewLine + "Details: " + additionalDetails : string.Empty))
        {

        }
    }
}
