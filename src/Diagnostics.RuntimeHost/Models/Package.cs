using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Publishing package
    /// </summary>
    public abstract class Package
    {
        private string _sanitizedCodeString;

        /// <summary>
        /// Gets or sets the code string.
        /// </summary>
        public string CodeString
        {
            get
            {
                return _sanitizedCodeString;
            }
            set
            {
                _sanitizedCodeString = FileHelper.SanitizeScriptFile(value);
            }
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the committed alias.
        /// </summary>
        public string CommittedByAlias { get; set; }

        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public string PackageConfig { get; set; }

        /// <summary>
        /// Get the commit for package.
        /// </summary>
        /// <returns>The commit</returns>
        public abstract Commit GetCommit();
    }
}
