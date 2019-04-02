namespace Diagnostics.DataProviders.Interfaces
{
    public interface IChangeAnalysisDataProvider
    {
        void GetResourceIds(string[] hostNames, string subscription);

        void GetChangeSets();

        void GetChanges();
    }
}
