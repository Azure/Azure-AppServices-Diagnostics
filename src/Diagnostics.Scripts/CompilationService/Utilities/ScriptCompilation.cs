using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Diagnostics.Scripts.Utilities
{
    public static class ScriptCompilation
    {
        /// <summary>
        /// Checks if compilation is required - if the script has changed or assembly is not loaded, compilation is done
        /// </summary>
        public static bool IsCompilationNeeded(string assembly, string script, string scriptETag, out Assembly loadedAssembly)
        {
            loadedAssembly = null;
            return !IsSameScript(script, scriptETag) || !IsAssemblyLoaded(assembly, out loadedAssembly);
        }

        /// <summary>
        /// Checks if the given assembly is already loaded
        /// </summary>
        public static bool IsAssemblyLoaded(string givenAssembly, out Assembly loadedAssembly)
        {
            loadedAssembly = null;
            if(string.IsNullOrEmpty(givenAssembly))
            {
                return false;
            }
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName == givenAssembly)
                {
                    loadedAssembly = assembly; 
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the script has changed by comparing the hash of new script and given hash
        /// </summary>
        public static bool IsSameScript(string script, string scriptHash)
        {
            if(string.IsNullOrEmpty(script))
            {
                return true;
            }
            byte[] oldHash = Convert.FromBase64String(scriptHash);
            byte[] newHash = GetHashFromScript(script);
            bool areEqual = false;
            if(oldHash.Length == newHash.Length)
            {
                int i = 0;
                while ((i < newHash.Length) && (newHash[i] == oldHash[i]))
                {
                    i++;
                }
                if(i == newHash.Length)
                {
                    areEqual = true;
                }
            }
            return areEqual;
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
