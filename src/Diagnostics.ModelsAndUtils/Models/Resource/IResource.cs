using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public interface IResource
    {
        string SubscriptionId { get; set; }

        string ResourceGroup { get; set; }

        string Name { get; set; }
    }
}
