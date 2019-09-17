using System;

namespace Diagnostics.ModelsAndUtils.Exceptions
{
    public class ASETenantListEmptyException: Exception
    {
        public ASETenantListEmptyException(string sourceName, string message): base($"Exception occurred in {sourceName}. {message}")
        {
        } 
    }
}