using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    public interface IMetadataProvider
    {
        DataProviderMetadata GetMetadata();
    }
}
