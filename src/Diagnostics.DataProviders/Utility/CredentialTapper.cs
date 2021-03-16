using System;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Traps potential creds and obfuscate them.
    /// </summary>
    public static class CredentialTrapper
    {
        private static readonly char[] ValueTerminators = new char[] { '<', '"' };
        private static readonly string[] CredentialTokens = new string[] { "Token=", "DefaultEndpointsProtocol=http", "AccountKey=", "Data Source=", "Server=", "Password=", "pwd=", "&amp;sig=", "&sig=", "sig=", "COMPOSE|", "KUBE|" };
        private const string SecretReplacement = "!!!SECRET-TRAP!!!";

        /// <summary>
        /// Masks out credentials in the input string based on predefined rules
        /// </summary>
        /// <param name="input">The input text with secrets</param>
        /// <returns>The input text with masked out secrets</returns>
        public static string Obfuscate(string input)
        {
            string temp = input;
            foreach (var token in CredentialTokens)
            {
                int startIndex = 0;
                while (true)
                {
                    // search for the next token instance
                    startIndex = temp.IndexOf(token, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (startIndex == -1)
                    {
                        break;
                    }

                    // Find the end of the secret. It most likely ends with either a double quota " or tag opening <
                    int credentialEnd = temp.IndexOfAny(ValueTerminators, startIndex);

                    temp = temp.Substring(0, startIndex) + SecretReplacement + (credentialEnd != -1 ? temp.Substring(credentialEnd) : string.Empty);
                }
            }

            return temp;
        }
    }
}
