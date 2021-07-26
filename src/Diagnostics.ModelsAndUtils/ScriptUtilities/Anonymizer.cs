using System;
using System.Text.RegularExpressions;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public static class Anonymizer
    {
        private const string DigitRegularExpression = @"\d";
        private const string EmailRegularExpression = @"(?<=[\w]{0})[\w-.+%]*(?=@([\w-]+)[.]{0})";
        private const string GuidRegularExpression = @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}";

        public static string AnonymizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            var array = message.Split(' ');
            for (int i = 0; i < array.Length; i++)
            {
                var item = array[i];
                item = ReplaceIfGuid(item);
                item = ReplaceIfEmail(item);
                item = ReplaceIfDigit(item);
                array[i] = item;
            }

            return string.Join(" ", array);
        }

        private static string ReplaceIfGuid(string input)
        {
            return Regex.Replace(input, GuidRegularExpression, "{{guid}}");
        }

        private static string ReplaceIfDigit(string input)
        {
            var output = Regex.Replace(input, DigitRegularExpression, "n");
            return output;
        }

        private static string ReplaceIfEmail(string input)
        {
            if (input.Contains("@", StringComparison.OrdinalIgnoreCase))
            {
                if (Regex.IsMatch(input, EmailRegularExpression, RegexOptions.IgnoreCase))
                {
                    return "{{PiiEmail}}";
                }
            }
            return input;
        }
    }
}
