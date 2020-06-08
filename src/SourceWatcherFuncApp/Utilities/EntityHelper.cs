using System;
using System.Reflection;
using System.IO;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.Storage;
using System.Linq;

namespace SourceWatcherFuncApp.Utilities
{
    public static class EntityHelper
    {
        public static DiagEntity PrepareEntityForLoad(Stream streamAssemblyData, string detectorScript, DiagEntity detectorPackage)
        {
            byte[] assemblyData = GetByteFromStream(streamAssemblyData);
            Assembly temp = Assembly.Load(assemblyData);

            if (!Enum.TryParse(detectorPackage.EntityType, true, out EntityType entityType))
            {
                entityType = EntityType.Signal;
            }
            EntityMetadata metaData = new EntityMetadata(detectorScript, entityType);
            using (var invoker = new EntityInvoker(metaData, ScriptHelper.GetFrameworkReferences(), ScriptHelper.GetFrameworkImports()))
            {
                invoker.InitializeEntryPoint(temp);
                var resourceFilter = invoker.ResourceFilter;
                detectorPackage.IsInternal = resourceFilter != null? resourceFilter.InternalOnly : false;
                detectorPackage.SupportTopicList = invoker.EntryPointDefinitionAttribute != null ? invoker.EntryPointDefinitionAttribute.SupportTopicList : new List<SupportTopic>() ;
                detectorPackage.AnalysisTypes = invoker.EntryPointDefinitionAttribute != null ? invoker.EntryPointDefinitionAttribute.AnalysisTypes : new List<string>();
                detectorPackage.DetectorType = invoker.EntryPointDefinitionAttribute != null ? invoker.EntryPointDefinitionAttribute.Type.ToString() : "Detector";
                if(invoker.ResourceFilter != null)
                {
                    if (invoker.ResourceFilter is AppFilter)
                    {
                        // Store WebApp related info
                        var resourceInfo = invoker.ResourceFilter as AppFilter;

                        AppType appType = resourceInfo.AppType;
                        var appTypesList = Enum.GetValues(typeof(AppType)).Cast<AppType>().Where(p => appType.HasFlag(p)).Select(x => Enum.GetName(typeof(AppType), x));
                        detectorPackage.AppType = string.Join(",", appTypesList);

                        PlatformType platformType = resourceInfo.PlatformType;
                        var platformTypesList = Enum.GetValues(typeof(PlatformType)).Cast<PlatformType>().Where(p => platformType.HasFlag(p)).Select(x => Enum.GetName(typeof(PlatformType), x));
                        detectorPackage.PlatForm = string.Join(",", platformTypesList);

                        StackType stackType = resourceInfo.StackType;
                        var stackTypesList = Enum.GetValues(typeof(StackType)).Cast<StackType>().Where(s => stackType.HasFlag(s)).Select(x => Enum.GetName(typeof(StackType), x));
                        detectorPackage.StackType = string.Join(",", stackTypesList);

                        detectorPackage.ResourceProvider = "Microsoft.Web";
                        detectorPackage.ResourceType = "sites";
                    } 
                    else if (invoker.ResourceFilter is ApiManagementServiceFilter)
                    {
                        detectorPackage.ResourceProvider = "Microsoft.ApiManagement";
                        detectorPackage.ResourceType = "service";
                    } 
                    else if (invoker.ResourceFilter is AppServiceCertificateFilter)
                    {
                        detectorPackage.ResourceProvider = "Microsoft.CertificateRegistration";
                        detectorPackage.ResourceType = "certificateOrders";
                    } 
                    else if (invoker.ResourceFilter is AppServiceDomainFilter)
                    {
                        detectorPackage.ResourceProvider = "Microsoft.DomainRegistration";
                        detectorPackage.ResourceType = "domains";
                    } 
                    else if (invoker.ResourceFilter is AzureKubernetesServiceFilter)
                    {
                        detectorPackage.ResourceProvider = "Microsoft.ContainerService";
                        detectorPackage.ResourceType = "managedClusters";
                    }
                    else if (invoker.ResourceFilter is LogicAppFilter)
                    {
                        detectorPackage.ResourceProvider = "Microsoft.Logic";
                        detectorPackage.ResourceType = "workflows";
                    }
                    else if (invoker.ResourceFilter is HostingEnvironmentFilter)
                    {
                        // Store ASE related info
                        var resourceInfo = invoker.ResourceFilter as HostingEnvironmentFilter;

                        PlatformType platformType = resourceInfo.PlatformType;
                        var platformTypesList = Enum.GetValues(typeof(PlatformType)).Cast<PlatformType>().Where(p => platformType.HasFlag(p)).Select(x => Enum.GetName(typeof(PlatformType), x));
                        detectorPackage.PlatForm = string.Join(",", platformTypesList);

                        HostingEnvironmentType hostingEnvironmentType = resourceInfo.HostingEnvironmentType;
                        var hostingEnvironmentTypesList = Enum.GetValues(typeof(HostingEnvironmentType)).Cast<HostingEnvironmentType>().Where(h => hostingEnvironmentType.HasFlag(h)).Select(x => Enum.GetName(typeof(HostingEnvironmentType), x));
                        detectorPackage.HostingEnvironmentType = string.Join(",", hostingEnvironmentTypesList);

                        detectorPackage.ResourceProvider = "Microsoft.Web";
                        detectorPackage.ResourceType = "hostingEnvironments";
                    }
                    else if (invoker.ResourceFilter is ArmResourceFilter)
                    {
                        // Store Provider and ResourceType
                        var resourceInfo = invoker.ResourceFilter as ArmResourceFilter;
                        detectorPackage.ResourceType = resourceInfo.ResourceTypeName;
                        detectorPackage.ResourceProvider = resourceInfo.Provider;
                        detectorPackage.ResourceType = resourceInfo.ResourceTypeName;
                    }
                }
                
            }
            detectorPackage.PartitionKey = detectorPackage.EntityType;
            detectorPackage.RowKey = detectorPackage.DetectorId;
            return detectorPackage;

        }

        
        public static byte[] GetByteFromStream(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
