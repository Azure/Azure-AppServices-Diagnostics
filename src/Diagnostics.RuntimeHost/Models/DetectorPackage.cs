
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Publishing package.
    /// </summary>
    public class DetectorPackage : Package
    {
        /// <summary>
        /// Gets or sets the dll bytes.
        /// </summary>
        public string DllBytes { get; set; }

        /// <summary>
        /// Gets or sets the pdb bytes.
        /// </summary>
        public string PdbBytes { get; set; }

        /// <summary>
        /// Get commit for detector package.
        /// </summary>
        /// <returns>The commit.</returns>
        public override Commit GetCommit()
        {
            var content = new Dictionary<string, Tuple<string, bool>>();
            var detectorFilePath = $"{Id.ToLower()}/{Id.ToLower()}";
            var csxFilePath = $"{detectorFilePath}.csx";
            var dllFilePath = $"{detectorFilePath}.dll";
            var pdbFilePath = $"{detectorFilePath}.pdb";
            var configPath = $"{Id.ToLower()}/package.json";

            content.Add(csxFilePath, Tuple.Create(CodeString, true));
            content.Add(dllFilePath, Tuple.Create(DllBytes, false));
            content.Add(pdbFilePath, Tuple.Create(PdbBytes, false));
            content.Add(configPath, Tuple.Create(PackageConfig, true));

            return new Commit
            {
                Message = $@"Detector : {Id.ToLower()}, CommittedBy : {CommittedByAlias}",
                Content = content.ToImmutableDictionary()
            };
        }
    }
}
