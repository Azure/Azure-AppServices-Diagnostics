using System;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public abstract class ResourceFilterBase : Attribute, IResourceFilter
    {
        /// <summary>
        /// Defines a resource type
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Defines whether the detector is only accessible internally via applens or externally too via azure portal, cli etc.
        /// </summary>
        public bool InternalOnly { get; set; }

        public ResourceFilterBase(ResourceType resourceType, bool internalOnly)
        {
            this.ResourceType = resourceType;
            this.InternalOnly = internalOnly;
        }
    }
}
