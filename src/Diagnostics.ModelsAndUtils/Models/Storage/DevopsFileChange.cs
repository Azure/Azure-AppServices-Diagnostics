﻿namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    public class DevopsFileChange
    {
        /// <summary>
        /// Commit id 
        /// </summary>
        public string CommitId { get; set; }

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
    }
}
