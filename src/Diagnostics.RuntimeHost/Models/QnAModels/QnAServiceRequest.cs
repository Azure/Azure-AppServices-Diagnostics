using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models.QnAModels
{
    public class QnAServiceRequest
    {
        public QnAOperationType OperationType;

        public bool IsDetectorPublic;

        public Definition DetectorDefinition;

        public QnAResourceType QnAResourceType;

        public QnAServiceRequest()
        {
        }

        public QnAServiceRequest(EntityInvoker invoker, QnAOperationType operationType, QnAResourceType resourceType)
        {
            this.OperationType = operationType;
            this.QnAResourceType = resourceType;
            this.IsDetectorPublic = !invoker.ResourceFilter.InternalOnly;
            this.DetectorDefinition = invoker.EntryPointDefinitionAttribute;
        }
    }
}
