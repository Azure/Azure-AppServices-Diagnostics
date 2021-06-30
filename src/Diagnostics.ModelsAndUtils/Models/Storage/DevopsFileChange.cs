using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.Storage
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
    }
}
