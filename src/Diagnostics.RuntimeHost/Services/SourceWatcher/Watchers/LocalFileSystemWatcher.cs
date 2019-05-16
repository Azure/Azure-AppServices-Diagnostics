using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class LocalFileSystemWatcher : SourceWatcherBase
    {
        private string _localScriptsPath;
        private Task _firstTimeCompletionTask;

        protected override Task FirstTimeCompletionTask => _firstTimeCompletionTask;

        protected override string SourceName => "LocalFileSystem";

        public LocalFileSystemWatcher(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache)
            : base(env, configuration, invokerCache, gistCache, "LocalFileSystemWatcher")
        {
            LoadConfigurations();
            Start();
        }
        
        public override void Start()
        {
            _firstTimeCompletionTask = StartWatcherInternal();
        }

        public override Task CreateOrUpdatePackage(Package pkg)
        {
            throw new NotImplementedException("Local Source Watcher Mode right now doesnt support live detector deployment.");
        }

        private async Task StartWatcherInternal()
        {
            try
            {
                LogMessage("SourceWatcher : Start");

                DirectoryInfo srcDirectoryInfo = new DirectoryInfo(_localScriptsPath);
                foreach (DirectoryInfo srcSubDirInfo in srcDirectoryInfo.GetDirectories())
                {
                    LogMessage($"Scanning in folder : {srcSubDirInfo.FullName}");
                    var files = srcSubDirInfo.GetFiles().OrderByDescending(p => p.LastWriteTimeUtc);
                    var csxFile = files.FirstOrDefault(p => p.Extension.Equals(".csx", StringComparison.OrdinalIgnoreCase));
                    var asmFile = files.FirstOrDefault(p => p.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase));
                    var packageJsonFile = files.FirstOrDefault(p => p.Name.Equals("package.json", StringComparison.CurrentCultureIgnoreCase));
                    var metadataFile = files.FirstOrDefault(p => p.Name.Equals("metadata.json", StringComparison.OrdinalIgnoreCase));

                    string scriptText = string.Empty;
                    if (csxFile != default(FileInfo))
                    {
                        scriptText = await File.ReadAllTextAsync(csxFile.FullName);
                    }

                    EntityType entityType = EntityType.Signal;
                    if (packageJsonFile != null)
                    {
                        var configFile = await FileHelper.GetFileContentAsync(packageJsonFile.FullName);
                        var config = JsonConvert.DeserializeObject<PackageConfig>(configFile);
                        entityType = string.Equals(config.Type, "gist", StringComparison.OrdinalIgnoreCase) ? EntityType.Gist : EntityType.Signal;
                    }

                    string metadata = string.Empty;
                    if (metadataFile != default(FileInfo))
                    {
                        metadata = await File.ReadAllTextAsync(metadataFile.FullName);
                    }

                    EntityMetadata scriptMetadata = new EntityMetadata(scriptText, EntityType.Signal, metadata);
                    EntityInvoker invoker = new EntityInvoker(scriptMetadata);

                    if (asmFile == default(FileInfo))
                    {
                        LogWarning($"No Assembly file (.dll). Skipping cache update");
                        continue;
                    }

                    LogMessage($"Loading assembly : {asmFile.FullName}");
                    Assembly asm = Assembly.LoadFrom(asmFile.FullName);

                    invoker.InitializeEntryPoint(asm);
                    LogMessage($"Updating cache with  new invoker with id : {invoker.EntryPointDefinitionAttribute.Id}");
                    _invokerCache.AddOrUpdate(invoker.EntryPointDefinitionAttribute.Id, invoker);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, ex);
            }
            finally
            {
                LogMessage("SourceWatcher : End");
            }
        }
        
        private void LoadConfigurations()
        {
            if (_env.IsProduction())
            {
                _localScriptsPath = (string)Registry.GetValue(RegistryConstants.LocalWatcherRegistryPath, RegistryConstants.LocalScriptsPathKey, string.Empty);
            }
            else
            {
                _localScriptsPath = (_config[$"SourceWatcher:Local:{RegistryConstants.LocalScriptsPathKey}"]).ToString();
            }

            if (!Directory.Exists(_localScriptsPath))
            {
                throw new DirectoryNotFoundException($"Script Source Directory : {_localScriptsPath} not found.");
            }
        }
    }
}
