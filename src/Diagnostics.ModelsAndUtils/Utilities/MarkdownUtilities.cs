using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Utilities
{
    public static class MarkdownUtilities
    {
        public static string ToMarkdownList(this Dictionary<string, object> input, string indent = " ")
        {
            var markdownBuilder = new StringBuilder();

            foreach (var kvp in input)
            {
                var value = "";

                if (kvp.Value.GetType() != typeof(string))
                {
                    value = JsonConvert.SerializeObject(kvp.Value);
                }
                else
                {
                    value = kvp.Value.ToString();
                }

                if (value.Length > 17)
                {
                    value = value.Truncate(17);
                    value = $"{value}...";
                }

                markdownBuilder.AppendLine($"{indent}- {kvp.Key}: {value}");
            }

            return markdownBuilder.ToString();
        }
    }
}
