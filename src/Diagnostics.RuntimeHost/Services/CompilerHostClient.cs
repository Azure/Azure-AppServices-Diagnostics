using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Utilities;
using Diagnostics.Scripts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services
{
    public interface ICompilerHostClient
    {
        Task<CompilerResponse> GetCompilationResponse(string script, string entityType, IDictionary<string, string> references, string requestId = "");
    }

    public class CompilerHostClient : ICompilerHostClient, IDisposable
    {
        private SemaphoreSlim _semaphoreObject;
        private IHostingEnvironment _env;
        private IConfiguration _configuration;
        private string _compilerHostUrl;
        private HttpClient _httpClient;
        private bool _isComplierHostRunning;
        private int _processId;
        private string _dotNetProductName;
        private string _compilerHostBinaryLocation;
        private string _compilerHostPort;
        private int _pollingIntervalInSeconds;
        private long _processMemoryThresholdInMB;
        private string _eventSource;

        public CompilerHostClient(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
            _semaphoreObject = new SemaphoreSlim(1, 1);
            
            _httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = Int32.MaxValue
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _isComplierHostRunning = false;
            _processId = -1;
            _dotNetProductName = "dotnet";
            _eventSource = "CompilerHostClient";

            LoadConfigurations();

            if (string.IsNullOrWhiteSpace(_compilerHostBinaryLocation))
            {
                throw new ArgumentNullException("compilerHostBinaryLocation");
            }

            _compilerHostUrl = $@"http://localhost:{_compilerHostPort}";

            StartProcessMonitor();
        }
        
        public async Task<CompilerResponse> GetCompilationResponse(string script, string entityType, IDictionary<string, string> references, string requestId = "")
        {
            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, "Get Compilation : Waiting on semaphore ...");

            await _semaphoreObject.WaitAsync();
            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, "Get Compilation : Entered critical section ...");

            try
            {
                // If for any reason, compiler host is not running due to failures in process monitor, launch it before making a call.
                if (!_isComplierHostRunning)
                {
                    await LaunchCompilerHostProcess(requestId);
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_compilerHostUrl}/api/compilerhost")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(PrepareRequestBody(script, entityType, references)), Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(requestId))
                {
                    requestMessage.Headers.Add(HeaderConstants.RequestIdHeaderName, requestId);
                }

                HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    string errorResponse = await responseMessage.Content.ReadAsStringAsync();
                    HttpRequestException ex = new HttpRequestException($"Status Code : {responseMessage.StatusCode}, Content : {errorResponse}");
                    DiagnosticsETWProvider.Instance.LogCompilerHostClientException(requestId, _eventSource, string.Empty, ex.GetType().ToString(), ex.ToString());
                    throw ex;
                }

                return await responseMessage.Content.ReadAsAsyncCustom<CompilerResponse>();
            }
            finally
            {
                _semaphoreObject.Release();
                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, "Get Compilation : semaphore released.");
            }
        }

        private async Task LaunchCompilerHostProcess(string requestId)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _compilerHostBinaryLocation,
                    FileName = _dotNetProductName,
                    Arguments = $@"Diagnostics.CompilerHost.dll --urls {_compilerHostUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = false
                }
            };

            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId ?? string.Empty, _eventSource, $"Launching Compiler Host Process. Running \"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}\"");

            proc.Start();
            await WaitForCompilerHostToBeReady(requestId);

            if (!proc.HasExited)
            {
                _isComplierHostRunning = true;
                _processId = proc.Id;
                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, $"Compiler Host Process running with Pid : {_processId}");
            }
            else
            {
                string standardOutput = await proc.StandardOutput.ReadToEndAsync();
                DiagnosticsETWProvider.Instance.LogCompilerHostClientException(requestId, _eventSource, $"Failed to start Compiler Host Process. Standard Output : {standardOutput}", string.Empty, string.Empty);
            }
        }
        
        private async Task WaitForCompilerHostToBeReady(string requestId)
        {
            await RetryHelper.RetryAsync(() => CheckForHealthPing(requestId), _eventSource, requestId, 5, 1000);
        }

        private async Task<bool> CheckForHealthPing(string requestId)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_compilerHostUrl}/api/compilerhost/healthping");

            if (!string.IsNullOrWhiteSpace(requestId))
            {
                requestMessage.Headers.Add(HeaderConstants.RequestIdHeaderName, requestId);
            }

            HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                string errorResponse = await responseMessage.Content.ReadAsStringAsync();
                HttpRequestException ex = new HttpRequestException($"Status Code : {responseMessage.StatusCode}, Content : {errorResponse}");
                DiagnosticsETWProvider.Instance.LogCompilerHostClientWarning(requestId, _eventSource, "Compiler host health ping failed", ex.GetType().ToString(), ex.ToString());
                throw ex;
            }

            return true;
        }

        private async void StartProcessMonitor()
        {
            while (true)
            {
                try
                {
                    Process proc = null;
                    if (_processId != -1)
                    {
                        proc = Process.GetProcessById(_processId);
                    }

                    if (proc != null && !proc.HasExited)
                    {
                        if (proc.WorkingSet64 > (_processMemoryThresholdInMB * 1024 * 1024))
                        {
                            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, $"Compiler Host memory Threshold reached. Pid : {proc.Id} Process Working Set(bytes) : {proc.WorkingSet64}, Threshold Memory(bytes) : {_processMemoryThresholdInMB * 1024 * 1024}");
                            try
                            {
                                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, "Restart Compiler Host : Waiting on semaphore ...");

                                await _semaphoreObject.WaitAsync();

                                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, "Restart Compiler Host : Entered critical section ...");

                                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, $"killing dotnet process with id : {proc.Id}");
                                proc.Kill();
                                proc.WaitForExit();
                                _processId = -1;
                                _isComplierHostRunning = false;

                                // Re-launch Compiler host for it be always running.
                                await LaunchCompilerHostProcess(string.Empty);
                            }
                            finally
                            {
                                _semaphoreObject.Release();
                                DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, "Restart Compiler Host : semaphore released.");
                            }
                        }
                    }
                    else
                    {
                        _processId = -1;
                        _isComplierHostRunning = false;

                        // Launch Compiler Host if not running.
                        try
                        {
                            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, "Start Compiler Host : Waiting on semaphore ...");
                            await _semaphoreObject.WaitAsync();
                            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, $"Start Compiler Host : Entered critical section ...");
                            await LaunchCompilerHostProcess(string.Empty);
                        }
                        finally
                        {
                            _semaphoreObject.Release();
                            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(string.Empty, _eventSource, "Start Compiler Host : semaphore released.");
                        }

                    }

                    await Task.Delay(_pollingIntervalInSeconds * 1000);
                }
                catch (Exception ex)
                {
                    string exceptionType = ex != null ? ex.GetType().ToString() : string.Empty;
                    string exceptionDetails = ex != null ? ex.ToString() : string.Empty;

                    DiagnosticsETWProvider.Instance.LogCompilerHostClientException(string.Empty, _eventSource, ex.Message, exceptionType, exceptionDetails);
                }
            }
        }

        private object PrepareRequestBody(string scriptText, string entityType, IDictionary<string, string> references)
        {
            return new
            {
                script = scriptText,
                entityType = entityType,
                reference = references
            };
        }

        private void LoadConfigurations()
        {
            // TODO : Probably needs a better way to manage configurations accross various services.
            if (_env.IsProduction())
            {
                _compilerHostBinaryLocation = (string)Registry.GetValue(RegistryConstants.CompilerHostRegistryPath, RegistryConstants.CompilerHostBinaryLocationKey, string.Empty);
                _compilerHostPort = (string)Registry.GetValue(RegistryConstants.CompilerHostRegistryPath, RegistryConstants.CompilerHostPortKey, string.Empty);
                _pollingIntervalInSeconds = Convert.ToInt32(Registry.GetValue(RegistryConstants.CompilerHostRegistryPath, RegistryConstants.CompilerHostPollingIntervalKey, 60));
                _processMemoryThresholdInMB = Convert.ToInt64(Registry.GetValue(RegistryConstants.CompilerHostRegistryPath, RegistryConstants.CompilerHostProcessMemoryThresholdInMBKey, 300));
            }
            else
            {
                _compilerHostBinaryLocation = (_configuration[$"CompilerHost:{RegistryConstants.CompilerHostBinaryLocationKey}"]).ToString();
                _compilerHostPort = (_configuration[$"CompilerHost:{RegistryConstants.CompilerHostPortKey}"]).ToString();
                _pollingIntervalInSeconds = Convert.ToInt32(_configuration[$"CompilerHost:{RegistryConstants.CompilerHostPollingIntervalKey}"]);
                _processMemoryThresholdInMB = Convert.ToInt64(_configuration[$"CompilerHost:{RegistryConstants.CompilerHostProcessMemoryThresholdInMBKey}"]);
            }
        }

        public void Dispose()
        {
            if(_semaphoreObject != null)
            {
                _semaphoreObject.Dispose();
            }

            if(_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}
