using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.Storage;
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

        public bool IsApplicable<TFilterType>(IResourceFilter incomingResourceFilter, string resourceProvider, string resourceTypeName, bool customFilterLogicResult = true) where TFilterType : IResourceFilter
        {
            bool isApplicable = incomingResourceFilter != null
                && incomingResourceFilter is TFilterType
                && incomingResourceFilter.ResourceType == this.ResourceType
                && customFilterLogicResult;

            return isApplicable || IsApplicableViaSharedProviderOrService(incomingResourceFilter, resourceProvider, resourceTypeName);
        }

        public bool IsApplicable(DiagEntity diagEntity, string resourceProvider, string resourceTypeName)
        {
            if (diagEntity == null || diagEntity.ResourceType == null || diagEntity.ResourceProvider == null)
            {
                return false;
            }

            ArmResourceFilter tempFilter = new ArmResourceFilter(diagEntity.ResourceProvider, diagEntity.ResourceType);
            return IsApplicableViaSharedProviderOrService(tempFilter, resourceProvider, resourceTypeName);
        }

        /// <summary>
        /// Returns True if the Detector/Gist is applicable (via sharable provider or service) for a resource.
        /// </summary>
        private bool IsApplicableViaSharedProviderOrService(IResourceFilter incomingResourceFilter, string resourceProvider, string resourceTypeName)
        {
            if (incomingResourceFilter == null)
            {
                return false;
            }

            bool isApplicable = false;

            // Check if this is a shared resource filter.
            if (incomingResourceFilter is ArmResourceFilter armResFilter && 
                !string.IsNullOrWhiteSpace(armResFilter.Provider) && 
                !string.IsNullOrWhiteSpace(armResFilter.ResourceTypeName))
            {
                isApplicable = (armResFilter.Provider == "*") ||
                                (armResFilter.Provider.Equals(resourceProvider, StringComparison.OrdinalIgnoreCase) && armResFilter.ResourceTypeName == "*") ||
                                (armResFilter.Provider.Equals(resourceProvider, StringComparison.OrdinalIgnoreCase) && armResFilter.ResourceTypeName.Equals(resourceTypeName, StringComparison.OrdinalIgnoreCase));
            }

            return isApplicable;
        }
    }
}
