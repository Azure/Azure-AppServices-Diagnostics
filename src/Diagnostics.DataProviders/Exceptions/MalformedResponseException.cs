using System;

namespace Diagnostics.DataProviders.Exceptions
{
    public class MalformedResponseException : Exception
    {
        public MalformedResponseException(string sourceName, string message): base($"Exception occurred in {sourceName}. {message}")
        {
        } 
    }
}