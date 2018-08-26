using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models.QnAModels
{
    public class QnAOperation
    {
        public string OperationState;

        public DateTime CreatedTimestamp;

        public DateTime LastActionTimestamp;

        public string ResourceLocation;

        public string UserId;

        public string OperationId;
    }
}
