using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

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

        public static bool ValidateRequestBody(IRequestBodyBase body, out string outputMessage)
        {
            outputMessage = "";
            if (body == null)
            {
                outputMessage = "Missing request body";
                return false;
            }

            List<string> missingProperties = new List<string>();

            foreach (var property in body.GetType().GetProperties())
            {
                if (property.GetValue(body) == null)
                {
                    missingProperties.Add(property.Name);
                }
            }

            if (missingProperties.Count() == 0)
                return true;


            var message = "The following fields are missing from the request body: ";
            outputMessage = message + String.Join(", ", missingProperties);

            return false;
        }
    }
}
