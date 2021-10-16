using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Diagnostics.DataProviders.Utility
{
    public class DataRedaction
    {
        private const string DigitRegularExpression = @"\d";
        private const string EmailRegularExpression = @"(?<=[\w]{1})[\w-\._\+%]*(?=@([\w-_]+)[\.]{0})";
        private const string GuidRegularExpression = @"(?im)[{(\-]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}\-]?";
        private const string PasswordRegularExpression = @"(?<=(\bpass\b)|(\bpwd\b)|(\bpassword\b)|(\buserpass\b))[^\w\r\n]+(.+)";
        private const string IPV4RegularExpression = @"(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9][0-9]|[0-9])";
        // Regex for validate US Phone number only
        private const string PhoneRegularExpression = @"^(\([0-9]{3}\)|[0-9]{3}-)[0-9]{3}-[0-9]{4}$";

        private const string InvalidUriAndContainsPossibleSensitiveInfo = "?message=The original string was removed completely as it could not be identified as a url for proper redaction and may contain sensitive information.";

        private const string RedactedValue = "***";

        private static string RedactQueryString(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute)
                       ? RedactQueryString(new Uri(url, UriKind.RelativeOrAbsolute))
                       : PrecautionaryRedaction(url);
        }

        private string RedactIPV4Address(string content)
        {
            string pattern = @"(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9][0-9]|[0-9])";
            return Regex.Replace(content, pattern, m => (m.Value.Split('.')[0] + "." + m.Value.Split('.')[1] + "." + m.Value.Split('.')[2] + ".XXX"));
        }


        private string RedactEmails(string content)
        {
            // (?<=[\w]{1}) the name has to start with 1 word-character
            // [\w-\._\+%]* the replacement-part can contain 0-n word characters including -_.+%
            // ?=@ the name has to end with a @
            // [\w-_]+ @ should be followed by one characters which may contain _
            // [\.]{0} Presence of a . is optional. To check for emails of the form abc@gmail
            // Depending on the amount of characters you want to remain unchanged you can change {1} to {2} or something else at the beginning or at the end.

            // Matches aar9_onb@dev.com, m@text.com.in, me@t_o.dom, user@gmail, again@gmail.
            // Does not match someone@, @someone
            return Regex.Replace(content, EmailRegularExpression, "[PIIEmail]");
        }

        private string RedactPassword(string content)
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
            return Regex.Replace(content, pattern, m => "c", RegexOptions.IgnoreCase);
        }

        public static string RedactGuid(string s)
        {
            return Regex.Replace(s, GuidRegularExpression, "[GUID]");
        }

        private string RedactPhoneNumber(string content)
        {
            string pattern = @"(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9][0-9]|[0-9])";
            return Regex.Replace(content, PhoneRegularExpression, m => (m.Value.Split('-')[-1] + "." + m.Value.Split('.')[1] + "." + m.Value.Split('.')[2] + ".XXX"));
        }
    }
}
