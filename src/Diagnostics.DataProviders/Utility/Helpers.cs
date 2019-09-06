using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Utility
{
    public static class Helpers
    {
        public static string GetKeyvaultforEnvironment(string hostingEnvironment)
        {
            switch (hostingEnvironment)
            {
                case "Production":
                    return "Secrets:ProdKeyVaultName";
                case "Staging":
                    return "Secrets:StagingKeyVaultName";
                case "Development":
                default:
                    return "Secrets:DevKeyVaultName";
            }
        }
    }
}
