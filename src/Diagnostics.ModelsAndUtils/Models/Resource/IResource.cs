using Diagnostics.ModelsAndUtils.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Interface defining an resource
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// Subscription Id.
        /// </summary>
        string SubscriptionId { get; set; }

        /// <summary>
        /// Resource Group Name.
        /// </summary>
        string ResourceGroup { get; set; }

        /// <summary>
        /// Resource Name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Type of Resource
        /// </summary>
        ResourceType ResourceType { get; set; }
        
        /// <summary>
        /// Determines whether the resource is applicable after filtering.
        /// </summary>
        /// <param name="filter">Resource Filter</param>
        /// <returns>True, if resource passes the filter. False otherwise</returns>
        bool IsApplicable(IResourceFilter filter);
    }
}