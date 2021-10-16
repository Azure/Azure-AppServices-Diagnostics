using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Utility
{
    class DataAnonymization
    {
        public const string InvalidUriAndContainsPossibleSensitiveInfo = "?message=The original string was removed completely as it could not be identified as a url for proper redaction and may contain sensitive information.";

        private const string RedactedValue = "***";

        public static string RedactQueryString(this string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute)
                       ? RedactQueryString(new Uri(url, UriKind.RelativeOrAbsolute))
                       : PrecautionaryRedaction(url);
        }
    }
}
