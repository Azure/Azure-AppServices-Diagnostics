using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Diagnostics.Scripts.CompilationService.ReferenceResolver
{
    /// <summary>
    /// Memory reference resolver
    /// </summary>
    public class MemoryReferenceResolver : SourceReferenceResolver
    {
        private readonly IDictionary<string, string> references;

        /// <summary>
        /// Gets or sets the used references.
        /// </summary>
        public ISet<string> Used { get; set; }

        public MemoryReferenceResolver(IDictionary<string, string> references)
        {
            Used = new HashSet<string>();
            this.references = references;
        }

        public override bool Equals(object other)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return references.GetHashCode();
        }

        public override string NormalizePath(string path, string baseFilePath)
        {
            return string.Empty;
        }

        public override Stream OpenRead(string resolvedPath)
        {
            var reference = references.ContainsKey(resolvedPath) ? references[resolvedPath] : "";

            return new MemoryStream(Encoding.UTF8.GetBytes(reference ?? ""));
        }

        public override string ResolveReference(string path, string baseFilePath)
        {
            if (!Used.Contains(path))
            {
                Used.Add(path);
            }

            return path;
        }
    }
}
