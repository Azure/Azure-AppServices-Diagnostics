using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Diagnostics.DataProviders.Interfaces;

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

        public static string MakeQueryCloudAgnostic(IKustoMap kustoMap, string query)
        {
            var matches = Regex.Matches(query, @"cluster\((?<cluster>(.+))\).database\((?<database>(.+))\)\.");

            if (matches.Any())
            {
                foreach (Match element in matches)
                {
                    var targetCluster = kustoMap.MapCluster(element.Groups["cluster"].Value.Trim(new char[] { '\'', '\"' }));
                    var targetDatabase = kustoMap.MapDatabase(element.Groups["database"].Value.Trim(new char[] { '\'', '\"' }));

                    if (!string.IsNullOrWhiteSpace(targetCluster) && !string.IsNullOrWhiteSpace(targetDatabase))
                    {
                        query = query.Replace($"cluster({element.Groups["cluster"].Value})", $"cluster('{targetCluster}')");
                        query = query.Replace($"database({element.Groups["database"].Value})", $"database('{targetDatabase}')");
                    }
                }
            }

            return query;
        }
    }
}
