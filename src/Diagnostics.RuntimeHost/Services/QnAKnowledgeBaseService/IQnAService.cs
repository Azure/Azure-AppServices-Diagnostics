using Diagnostics.RuntimeHost.Models.QnAModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IQnAService
    {
        void EnqueueQnAServiceRequest(QnAServiceRequest request);
    }
}
