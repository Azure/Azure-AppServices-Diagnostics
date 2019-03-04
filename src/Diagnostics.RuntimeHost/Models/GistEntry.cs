
namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Gist cache entry.
    /// </summary>
    public class GistEntry
    {
        /// <summary>
        /// Gets or sets the code string.
        /// </summary>
        public string CodeString { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the gist name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the authors.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets category.
        /// </summary>
        public string Category { get; set; }
    }
}
