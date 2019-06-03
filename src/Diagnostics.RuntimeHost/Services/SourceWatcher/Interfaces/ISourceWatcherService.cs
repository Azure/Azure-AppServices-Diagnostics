namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public interface ISourceWatcherService
    {
        ISourceWatcher Watcher { get; }
    }
}
