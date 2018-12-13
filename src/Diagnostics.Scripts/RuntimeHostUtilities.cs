using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.ModelsAndUtils.ScriptUtilities
{
    public class RuntimeHostUtilities
    {
        public static Task<object> InvokeDetector<TResource>(OperationContext<TResource> context, string detectorId, object dataProviders) where TResource : IResource
        {
            var _invokerCache = context.InvokerCacheService as InvokerCacheService;
            RuntimeContext<TResource> cxt = new RuntimeContext<TResource>()
            {
                ClientIsInternal = context.IsInternalCall,
                OperationContext = context
            };
            var invoker = _invokerCache.GetDetectorInvoker(detectorId, cxt);

            if (invoker == null)
            {
                return null;
            }

            Response res = new Response
            {
                Metadata = RemovePIIFromDefinition(invoker.EntryPointDefinitionAttribute, cxt.ClientIsInternal)
            };

            return invoker.Invoke(new object[] { dataProviders, context, res });
        }

        private static Definition RemovePIIFromDefinition(Definition definition, bool isInternal)
        {
            if (!isInternal) definition.Author = string.Empty;
            return definition;
        }
    }
}
