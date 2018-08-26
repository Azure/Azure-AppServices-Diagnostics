using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.RuntimeHost.Models.QnAModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    public static class QnAUtilities
    {
        public static QnAResourceType GetQnAResourceType(ResourceType resType)
        {
            switch (resType)
            {
                case ResourceType.ApiManagementService:
                    return QnAResourceType.APIM;
                case ResourceType.AppServiceCertificate:
                    return QnAResourceType.AppServiceCert;
                case ResourceType.AppServiceDomain:
                    return QnAResourceType.AppServiceDomain;
                case ResourceType.HostingEnvironment:
                    return QnAResourceType.ASE;
                case ResourceType.LogicApp:
                    return QnAResourceType.LogicApp;
                default:
                    return QnAResourceType.WebAppWindows;
            }
        }
    }
}
