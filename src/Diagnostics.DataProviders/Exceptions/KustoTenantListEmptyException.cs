using System;

namespace Diagnostics.DataProviders.Exceptions
{
    public class KustoTenantListEmptyException : Exception
    {
        public KustoTenantListEmptyException(string sourceName, string message): base($"Exception occurred in {sourceName}. {message}")
        {
        } 
    }
}