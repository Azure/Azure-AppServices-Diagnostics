using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Defines the properties of a detector definition
    /// </summary>
    public class Definition : Attribute, IEquatable<Definition>
    {
        /// <summary>
        /// Id of the detector(unique)
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// User-Friendly Name of the detector.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Description of the detector.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Author of the detector.
        /// </summary>
        [DataMember]
        public string Author { get; set; }

        /// <summary>
        /// List of Support Topics for which this detector is enabled.
        /// </summary>
        [DataMember]
        public IEnumerable<SupportTopic> SupportTopicList { get; set; }
        
        public bool Equals(Definition other)
        {
            return Id == other.Id;
        }
    }
}
