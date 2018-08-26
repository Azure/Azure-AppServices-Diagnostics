using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models.QnAModels
{
    public enum QnAResourceType
    {
        None,
        WebAppWindows,
        WebAppLinux,
        FunctionApp,
        ASE,
        LogicApp,
        APIM,
        AppServiceCert,
        AppServiceDomain
    }
}
