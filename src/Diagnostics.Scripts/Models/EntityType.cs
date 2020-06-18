namespace Diagnostics.Scripts.Models
{
    /// <summary>
    /// Entity type.
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// Signal entity.
        /// </summary>
        Signal = 1,

        /// <summary>
        /// Detector entity.
        /// </summary>
        Detector = 2,

        /// <summary>
        /// Analysis entity.
        /// </summary>
        Analysis = 4,

        /// <summary>
        /// Gist entity.
        /// </summary>
        Gist = 8
    }
}
