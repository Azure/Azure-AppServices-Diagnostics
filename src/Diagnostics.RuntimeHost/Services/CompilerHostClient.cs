using Diagnostics.ModelsAndUtils;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
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
        Task<CompilerResponse> GetCompilationResponse(string script);
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

            LoadConfigurations();

            if (string.IsNullOrWhiteSpace(_compilerHostBinaryLocation))
            {
                throw new ArgumentNullException("compilerHostBinaryLocation");
            }

            _compilerHostUrl = $@"http://localhost:{_compilerHostPort}";

            StartProcessMonitor();
        }
        
        public async Task<CompilerResponse> GetCompilationResponse(string script)
        {
            await _semaphoreObject.WaitAsync();
            
            try
            {
                if (!_isComplierHostRunning)
                {
                    await LaunchCompilerHostProcess();
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_compilerHostUrl}/api/compilerhost")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(PrepareRequestBody(script)), Encoding.UTF8, "application/json")
                };

                HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);

                // TODO : Check for 200 and handle errors

                return await responseMessage.Content.ReadAsAsyncCustom<CompilerResponse>();
            }
            finally
            {
                _semaphoreObject.Release();
            }
        }

        private async Task LaunchCompilerHostProcess()
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

            proc.Start();
            // TODO : Remove artificial wait.
            await Task.Delay(5000);

            if (!proc.HasExited)
            {
                _isComplierHostRunning = true;
                _processId = proc.Id;
            }
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
                            try
                            {
                                await _semaphoreObject.WaitAsync();
                                proc.Kill();
                                proc.WaitForExit();
                                _processId = -1;
                                _isComplierHostRunning = false;
                            }
                            finally
                            {
                                _semaphoreObject.Release();
                            }
                        }
                    }
                    else
                    {
                        _processId = -1;
                        _isComplierHostRunning = false;
                    }

                    await Task.Delay(_pollingIntervalInSeconds * 1000);
                }
                catch (Exception)
                {
                    // TODO : Log Exception
                }
            }
        }

        private object PrepareRequestBody(string scriptText)
        {
            return new
            {
                script = scriptText
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
