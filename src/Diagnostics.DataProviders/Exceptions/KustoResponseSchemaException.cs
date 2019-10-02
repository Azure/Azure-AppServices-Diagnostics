using System;

namespace Diagnostics.DataProviders.Exceptions
{
    public class KustoResponseSchemaException : Exception
    {
        public KustoResponseSchemaException(string sourceName, string message): base($"Exception occurred in {sourceName}. {message}")
        {
        } 
    }
}