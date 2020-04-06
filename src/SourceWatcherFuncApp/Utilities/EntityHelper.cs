using System;
using System.Reflection;
using System.IO;
using Diagnostics.Scripts;
using SourceWatcherFuncApp.Entities;
using Diagnostics.Scripts.Models;
using Diagnostics.ModelsAndUtils.ScriptUtilities;
using System.Collections.Generic;
using Diagnostics.ModelsAndUtils.Attributes;

namespace SourceWatcherFuncApp.Utilities
{
    public static class EntityHelper
    {
        public static DetectorEntity PrepareEntityForLoad(Stream streamAssemblyData, string detectorScript, DetectorEntity detectorPackage)
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
                detectorPackage.ResourceType = resourceFilter != null ? resourceFilter.ResourceType.ToString() : "";
                detectorPackage.SupportTopicList = invoker.EntryPointDefinitionAttribute != null ? invoker.EntryPointDefinitionAttribute.SupportTopicList : new List<SupportTopic>() ;
                detectorPackage.AnalysisTypes = invoker.EntryPointDefinitionAttribute != null ? invoker.EntryPointDefinitionAttribute.AnalysisTypes : new List<string>();
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
