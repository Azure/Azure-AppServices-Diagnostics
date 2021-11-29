using System;
using System.Text.RegularExpressions;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public static class DataAnonymizer
    {
        // TODO: Need to check if we want to redact digit numbers
        private static string RedactDigit(string input)
        {
            string pattern = @"\d";
            return Regex.Replace(input, pattern, "n");
        }
        private static string RedactGuid(string content)
        {
            string pattern = @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}";
            return Regex.Replace(content, pattern, "[guid]");
        }

        private static string RedactPhone(string content)
        {
            // (\+?\d ?\d ?\d ?) Matches an optional + sign and optional up to 3 digits
            //(\([0-9]{3}\)|[0-9]{3}-) Matches 3 digits or 3 digits followed by a "-"
            // ([0-9]{3}-[0-9]{4}) Matches 3 digits followed by "-" and 4 digits

            /*Matches
             * Phone: (425)222-7777 
             * Phone: 425-222-7777
             * saasd +1425-222-7777 
             */
            // Does not match :425333778988767

            string pattern = @"(\+?\d?\d?\d?)(\([0-9]{3}\)|[0-9]{3}-)([0-9]{3}-[0-9]{4})";
            return Regex.Replace(content, pattern, m => String.Concat(m.Value.Substring(0, m.Length - 4), "****"));
        }

        private static string RedactQueryString(string content)
        {
            // (?<=https?:\/\/([\w\.-_%]+\?) Starts with an http, may contain an s, must contain a :// followerd by at least one alphanumeric character including  .-_% and must end with a ? to indicate start of a query string.
            // [\w-\._&%]+) After a ?, there must be at least one alphanumeric character including - . _ & % followed by = (denoted by ?=). This matches the name part of the querystring.
            // ?=[\w-\._%]+ Repeatedly match a word (including - . _ % having at least one character that starts after an = .

            // Matches http://something.com?me=1&so=me https://something.co.uk?me=1&so=me&do=re&me=do%20something http://something.com?me=1 .
            // Does not match https://something.com? https://noQuery.com .

            string pattern = @"(?<=https?:\/\/([\w\.-_%]+\?)[\w-\._&%]+)?=[\w-\._%]+";
            return Regex.Replace(content, pattern, m => String.Concat(m.Value.Split('?')[0], "?****"), RegexOptions.IgnoreCase);
        }

        private static string RedactIPV4Address(string content)
        {
            string pattern = @"(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9][0-9]|[0-9])";
            return Regex.Replace(content, pattern, m => (m.Value.Split('.')[0] + "." + m.Value.Split('.')[1] + "." + m.Value.Split('.')[2] + ".XXX"));
        }

        private static string RedactIPV6Address(string content)
        {
            /* Matches
             *  2001:0db8:85a3:0000:0000:8a2e:0370:7334
             */
            string pattern = @"([a-f0-9:]+:+)+[a-f0-9]+";
            return Regex.Replace(content, pattern, m => "********");
        }

        private static string RedactPassword(string content)
        {
            // (?<=(\bpass\b)|(\bpwd\b)|(\bpassword\b)|(\buserpass\b)) Pattern should start with either pass or pwd or password or userpass.
            // [^\w\r\n]+ Followed by at least one character including space but not the newline character.
            // (.+) Match everything else and there should be at least one character present

            /* Matches
             * pass abc@#_!(?%123 
             * pass abc@#_!(?%123
             * password abc@#_!(?%123 
             * Password : abc@#_!(?%123
             * PASS abc@#_!(?%123
             * pass abc@#_!(?%123
             * password abc@#_!(?%123 sads adasd
            Does not match
             * Pass:
             * I am doing fine :)
             */
            string pattern = @"(?<=(\bpass\b)|(\bpwd\b)|(\bpassword\b)|(\buserpass\b))[^\w\r\n]+(.+)";
            return Regex.Replace(content, pattern, m => ":[PASSWORD]", RegexOptions.IgnoreCase);
        }

        private static string RedactEmails(string content)
        {
            // (?<=[\w]{1}) the name has to start with 1 word-character
            // [\w-\._\+%]* the replacement-part can contain 0-n word characters including -_.+%
            // ?=@ the name has to end with a @
            // [\w-_]+ @ should be followed by one characters which may contain _
            // [\.]{0} Presence of a . is optional. To check for emails of the form abc@gmail
            // Depending on the amount of characters you want to remain unchanged you can change {1} to {2} or something else at the beginning or at the end.

            // Matches aar9_onb@dev.com, m@text.com.in, me@t_o.dom, user@gmail, again@gmail.
            // Does not match someone@, @someone
            string pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=@([\w-_]+)[\.]{0})";
            return Regex.Replace(content, pattern, "***");
        }

        public static string RedactPII(string content)
        {
            string currContent = content;

            currContent = RedactEmails(currContent);
            currContent = RedactPhone(currContent);

            // Disable password regex matching to avoid high cpu issue
            //  currContent = RedactPassword(currContent);

            // Disable GUID and query string redaction for now
            // currContent = RedactQueryString(currContent);
            // currContent = RedactGuid(currContent);

            //TODO: Check if we want to redact IP addresses for now

            //currContent = RedactIPV4Address(currContent);
            //currContent = RedactIPV6Address(currContent);

            return currContent;
        }

        public static string AnonymizeContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var contentArray = content.Split(' ');

            for (int i = 0; i < contentArray.Length; i++)
            {
                var item = contentArray[i];

                // Skip data redact if string content is too long
                if (item.Length >= 500)
                    continue;

                // For now use this logic to simpily redact password to prevent high cpu issue caused by regex matching.
                if ((string.Equals(item, "pass", StringComparison.CurrentCultureIgnoreCase) || string.Equals(item, "pass:", StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(item, "password", StringComparison.CurrentCultureIgnoreCase) || string.Equals(item, "password:", StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(item, "userpass", StringComparison.CurrentCultureIgnoreCase) || string.Equals(item, "userpass:", StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(item, "pwd", StringComparison.CurrentCultureIgnoreCase) || string.Equals(item, "pwd:", StringComparison.CurrentCultureIgnoreCase))
                    && i + 1 < contentArray.Length)
                {
                    contentArray[i + 1] = ":[PASSWORD]";
                    i++;
                }
                else
                {
                    if (item.Contains("@") && item.Contains("."))
                    {
                        item = RedactEmails(item);
                    }

                    contentArray[i] = RedactPhone(item);
                }
            }

            return string.Join(" ", contentArray);
        }
    }
}