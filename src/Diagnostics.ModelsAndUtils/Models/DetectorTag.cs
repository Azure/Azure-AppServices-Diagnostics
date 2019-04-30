using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Used to mark a detector for a certain type of display or action.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DetectorTag
    {
        /// <summary>
        /// Detector is waiting to be validated.
        /// </summary>
        WaitingForValidation,

        /// <summary>
        /// Detector validation is overridden.
        /// </summary>
        OverrideValidation
    }
}
