using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Diagnostics.Scripts.Utilities
{
    public static class ScriptCompilation
    {
        /// <summary>
        /// Checks if the script has changed by comparing the hash of new script and given hash
        /// </summary>
        public static bool IsSameScript(string script, string scriptHash)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return true;
            }
            byte[] oldHash = Convert.FromBase64String(scriptHash);
            byte[] newHash = GetHashFromScript(script);
            return oldHash.SequenceEqual(newHash);
        }

        /// <summary>
        /// Computes hash of a script
        /// </summary>
        public static byte[] GetHashFromScript(string script)
        {
            return new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(script));
        }
    }
}
