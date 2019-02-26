
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Gist package.
    /// </summary>
    public class GistPackage : Package
    {
        /// <summary>
        /// Get commit for gist package.
        /// </summary>
        /// <returns>The commit.</returns>
        public override Commit GetCommit()
        {
            var content = new Dictionary<string, Tuple<string, bool>>();
            var csxFilePath = $"{Id.ToLower()}/{Id.ToLower()}.csx";
            var configPath = $"{Id.ToLower()}/package.json";

            content.Add(csxFilePath, Tuple.Create(CodeString, true));
            content.Add(configPath, Tuple.Create(PackageConfig, true));

            return new Commit
            {
                Message = $@"Gist : {Id.ToLower()}, CommittedBy : {CommittedByAlias}",
                Content = content.ToImmutableDictionary()
            };
        }
    }
}
