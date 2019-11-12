// <copyright file="GitHubWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Models;
using Diagnostics.RuntimeHost.Services.CacheService;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using Diagnostics.ModelsAndUtils.Attributes;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    /// <summary>
    /// Github watcher.
    /// </summary>
    public class CosmosDBWatcher : SourceWatcherBase
    {
        private Task _firstTimeCompletionTask;

        public readonly ISearchService _searchService;

        private readonly CosmosDBClient.CosmosDBClient _cosmosDBClient;

        // Load from configuration.
        private int _pollingIntervalInSeconds;

        private bool _loadOnlyPublicDetectors;

        private DateTime _lastModified;

        protected override Task FirstTimeCompletionTask => _firstTimeCompletionTask;

        protected override string SourceName => "AzureCosmosDB";

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubWatcher" /> class.
        /// </summary>
        /// <param name="env">Hosting environment.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="invokerCache">Invoker cache.</param>
        /// <param name="gistCache">Gist cache.</param>
        /// <param name="githubClient">Github client.</param>
        public CosmosDBWatcher(IHostingEnvironment env, IConfiguration configuration, IInvokerCacheService invokerCache, IGistCacheService gistCache, CosmosDBClient.CosmosDBClient cosmosDBClient, ISearchService searchService)
            : base(env, configuration, invokerCache, gistCache, "AzureCosmosDBWatcher")
        {
            _searchService = searchService;
            _lastModified = DateTime.MinValue;

            _cosmosDBClient = cosmosDBClient;
            //_blobStorageClient = blobStorageClient; // TODO

            LoadConfigurations();

            Start();
        }

        /// <summary>
        /// Start github watcher.
        /// </summary>
        public override void Start()
        {
            _firstTimeCompletionTask = StartWatcherInternal(true);
            StartPollingForChanges();
        }

        /// <summary>
        /// Start github watcher
        /// </summary>
        /// <returns>Task for starting watcher.</returns>
        private async Task StartWatcherInternal(bool startup)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                LogMessage("SourceWatcher : Start");

                var detectorData = await _cosmosDBClient.DetectorDataTable.GetItemsAsync(x => x.LastUpdated > _lastModified);

                if (detectorData.Count == 0)// no changes
                {
                    LogMessage("No Changes detected since last polling interval");
                    return;
                }

                List<Models.CosmosModels.SupportTopic> supportTopicTable;
                if (startup)
                {
                    // download all support toppics
                    supportTopicTable = await _cosmosDBClient.SupportTopicTable.GetItemsAsync();
                }
                else
                {
                    // download only support topics that are in the updated detectors // https://www.w3schools.com/Sql/sql_in.asp
                    // https://stackoverflow.com/questions/2334327/what-is-the-linq-equivalent-to-the-sql-in-operator
                    var changedDetectors = new string[detectorData.Count];
                    for (var i = 0; i < detectorData.Count; ++i)
                    {
                        changedDetectors[i] = detectorData[i].Id;
                    }

                    supportTopicTable = await _cosmosDBClient.SupportTopicTable.GetItemsAsync(x => changedDetectors.Contains(x.SupportTopicsId));
                }

                Dictionary<string, List<SupportTopic>> supportTopicMap = new Dictionary<string, List<SupportTopic>>();
                foreach(var supportTopic in supportTopicTable)
                {
                    if (!supportTopicMap.ContainsKey(supportTopic.DetectorId))
                    {
                        supportTopicMap[supportTopic.DetectorId] = new List<SupportTopic>();
                    }
                    supportTopicMap[supportTopic.DetectorId].Add(new SupportTopic() { Id = supportTopic.SupportTopicsId, PesId = supportTopic.PesId });
                }

                //create and update the items in the cache
                foreach (var detectorDatum in detectorData)
                {
                    if (detectorDatum.LastUpdated > _lastModified)
                    {
                        _lastModified = detectorDatum.LastUpdated;
                    }

                    var entityMetadata = new EntityMetadata(string.Empty, detectorDatum.EntityType, detectorDatum.Metadata);
                    IResourceFilter resourceFilter = detectorDatum.ResourceFilter;
                    supportTopicMap.TryGetValue(detectorDatum.Id, out var supportTopicList);
                    var definition = new Definition()
                    {
                        AnalysisType = detectorDatum.AnalysisType,
                        Author = detectorDatum.Author,
                        Category = detectorDatum.Category,
                        Description = detectorDatum.Description,
                        Id = detectorDatum.Id,
                        Name = detectorDatum.Name,
                        Type = detectorDatum.detectorType,
                        SupportTopicList = supportTopicList
                    };

                    var entityInvoker = new EntityInvoker(entityMetadata, definition, resourceFilter, detectorDatum.SystemFilterSpecifdied);

                    if (detectorDatum.IsGist)
                    {
                        _gistCache.AddOrUpdate(entityInvoker.EntryPointDefinitionAttribute.Id, entityInvoker);
                    }
                    else
                    {
                        _invokerCache.AddOrUpdate(entityInvoker.EntryPointDefinitionAttribute.Id, entityInvoker);
                    }
                }


            }
            catch (Exception ex)
            {
                LogException(ex.Message, ex);
            }
            finally
            {
                stopWatch.Stop();
                LogMessage($"SourceWatcher : End, Time Taken: {stopWatch.ElapsedMilliseconds}");
                Console.WriteLine($"SourceWatcher : End, Time Taken: {stopWatch.ElapsedMilliseconds}");
            }
        }

        private async void StartPollingForChanges()
        {
            await _firstTimeCompletionTask;

            do
            {
                await Task.Delay(_pollingIntervalInSeconds * 1000);
                await StartWatcherInternal(false);
            } while (true);
        }

        /// <summary>
        /// Create or update a package.
        /// </summary>
        /// <param name="pkg">Detector package.</param>
        /// <returns>Task for creating or updateing detector.</returns>
        public override async Task CreateOrUpdatePackage(Package pkg)
        {
            if (pkg == null)
            {
                throw new ArgumentNullException(nameof(pkg));
            }

            //TODO
            throw new NotImplementedException();
        }

        private void LoadConfigurations()
        {
            var pollingIntervalvalue = string.Empty;
            pollingIntervalvalue = (_config[$"SourceWatcher:{RegistryConstants.PollingIntervalInSecondsKey}"]).ToString();

            if (!bool.TryParse((_config[$"SourceWatcher:{RegistryConstants.LoadOnlyPublicDetectorsKey}"]), out _loadOnlyPublicDetectors))
            {
                _loadOnlyPublicDetectors = false;
            }

            if (!int.TryParse(pollingIntervalvalue, out _pollingIntervalInSeconds))
            {
                _pollingIntervalInSeconds = HostConstants.WatcherDefaultPollingIntervalInSeconds;
            }
        }
        protected static void LogException(string message, Exception ex)
        {
            var exception = new SourceWatcherException("Github", message, ex);
            DiagnosticsETWProvider.Instance.LogSourceWatcherException("GithubWatcher", message, exception.GetType().ToString(), exception.ToString());
        }
    }
}
