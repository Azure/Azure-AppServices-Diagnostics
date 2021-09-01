using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    public static class RequestBodyValidator
    {
        public static bool ValidateRequestBody(JToken jsonBody, string[] fieldNames, out string outputMessage)
        {
            if (jsonBody == null)
            {
                outputMessage = "Missing request body";
                return false;
            }

            List<string> validationResults = fieldNames.Select(field => jsonBody.SelectToken(field) == null ? field = $"Missing {field} from request body" : field = "valid").Distinct().Where(field => field != "valid").ToList();
            outputMessage = string.Concat(validationResults);
            return !(validationResults.Count() > 0);
        }
    }
}
