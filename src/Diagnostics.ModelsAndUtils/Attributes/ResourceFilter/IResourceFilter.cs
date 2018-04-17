using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    public interface IResourceFilter
    {
        ResourceType ResourceType { get; set; }

        bool InternalOnly { get; set; }
    }
}
