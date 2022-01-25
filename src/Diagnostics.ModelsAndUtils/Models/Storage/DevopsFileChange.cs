using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class DevopsFileChange
    {
        /// <summary>
        /// Commit id 
        /// </summary>
        public string CommitId { get; set; }

        /// <summary>
        /// Detector/gist id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Content of .csx file
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Path of the detector csx file
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Content of package.json file
        /// </summary>
        public string PackageConfig { get; set; }

        /// <summary>
        /// Content of metadata.json file
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Checks if the detector should be marked as disabled.
        /// </summary>
        public bool MarkAsDisabled { get; set; }
    }

    public class DevOpsPullRequest : IRequestBodyBase
    {
        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
        public string Title { get; set; }
        public string ResourceUri { get; set; }
    }

    public class DevOpsPushChangeRequest : IRequestBodyBase
    {
        public string Branch { get; set; }
        public IEnumerable<string> Files { get; set; }
        public IEnumerable<string> RepoPaths { get; set; }
        public string Comment { get; set; }
        public string ChangeType { get; set; }
        public string ResourceUri { get; set; }
    }
}
