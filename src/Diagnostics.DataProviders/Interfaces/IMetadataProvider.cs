using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    public interface IMetadataProvider
    {
        DataProviderMetadata GetMetadata();
    }
}
