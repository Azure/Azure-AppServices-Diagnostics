namespace Diagnostics.DataProviders
{
    public interface IDiagnosticDataProvider
    {
        IDataProviderConfiguration DataProviderConfiguration { get; }
    }
}
