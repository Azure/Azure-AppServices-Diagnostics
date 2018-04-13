using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public abstract class ResourceFilterBase: Attribute, IResourceFilter
    {
        /// <summary>
        /// Defines a resource type 
        /// </summary>
        public ResourceType ResourceType { get; set; }
        
        public ResourceFilterBase(ResourceType resourceType)
        {
            this.ResourceType = resourceType;
        }
    }
}
