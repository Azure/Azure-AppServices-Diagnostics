using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;

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

            _eventSource = "CompilerHostClient";

            LoadConfigurations();

            if (string.IsNullOrWhiteSpace(_compilerHostUrl))
            {
                throw new ArgumentNullException("compilerHostUrl");
            }
        }

        public async Task<CompilerResponse> GetCompilationResponse(string script, string entityType, IDictionary<string, string> references, string requestId = "")
        {
            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, "Get Compilation : Waiting on semaphore ...");

            await _semaphoreObject.WaitAsync();
            DiagnosticsETWProvider.Instance.LogCompilerHostClientMessage(requestId, _eventSource, "Get Compilation : Entered critical section ...");

            try
            {
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
                _compilerHostUrl = (string)Registry.GetValue(RegistryConstants.CompilerHostRegistryPath, RegistryConstants.CompilerHostUrlKey, string.Empty);
            }
            else
            {
                _compilerHostUrl = (_configuration[$"CompilerHost:{RegistryConstants.CompilerHostUrlKey}"]).ToString();
            }
        }

        public void Dispose()
        {
            if (_semaphoreObject != null)
            {
                _semaphoreObject.Dispose();
            }

            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}
