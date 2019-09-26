using System;

namespace Diagnostics.RuntimeHost.Models.Exceptions
{
    internal class ASETenantListEmptyException: Exception
    {
        public ASETenantListEmptyException(string sourceName, string message): base($"Exception occurred in {sourceName}. {message}")
        {
        } 
    }
}