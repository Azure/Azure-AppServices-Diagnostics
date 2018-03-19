using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public class LocalFileSystemWatcher : ISourceWatcher
    {
        private Task _firstTimeCompletionTask;
        private IHostingEnvironment _env;
        private IConfiguration _config;
        private ICache<string, EntityInvoker> _invokerCache;
        private string _localScriptsPath;

        public LocalFileSystemWatcher(IHostingEnvironment env, IConfiguration configuration, ICache<string, EntityInvoker> invokerCache)
        {
            _env = env;
            _config = configuration;
            _invokerCache = invokerCache;
            LoadConfigurations();
            Start();
        }

        public LocalFileSystemWatcher(string localScriptsSourcePath, ICache<string, EntityInvoker> invokerCache)
        {
            _localScriptsPath = localScriptsSourcePath;
            _invokerCache = invokerCache;
            Start();
        }

        public void Start()
        {
            _firstTimeCompletionTask = StartWatcherInternal();
        }
        
        public Task WaitForFirstCompletion() => _firstTimeCompletionTask;

        private async Task StartWatcherInternal()
        {
            DirectoryInfo srcDirectoryInfo = new DirectoryInfo(_localScriptsPath);
            foreach (DirectoryInfo srcSubDirInfo in srcDirectoryInfo.GetDirectories())
            {
                var files = srcSubDirInfo.GetFiles().OrderByDescending(p => p.LastWriteTimeUtc);
                var csxFile = files.FirstOrDefault(p => p.Extension.Equals(".csx", StringComparison.OrdinalIgnoreCase));
                var asmFile = files.FirstOrDefault(p => p.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase));

                string scriptText = string.Empty;
                if(csxFile != default(FileInfo))
                {
                    scriptText = await File.ReadAllTextAsync(csxFile.FullName);
                }

                EntityMetadata scriptMetadata = new EntityMetadata(scriptText);
                EntityInvoker invoker = new EntityInvoker(scriptMetadata);

                if(asmFile == default(FileInfo))
                {
                    // TODO : Log Error of missing dll
                    continue;
                }

                Assembly asm = Assembly.LoadFrom(asmFile.FullName);
                invoker.InitializeEntryPoint(asm);

                _invokerCache.AddOrUpdate(invoker.EntryPointDefinitionAttribute.Id, invoker);
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
