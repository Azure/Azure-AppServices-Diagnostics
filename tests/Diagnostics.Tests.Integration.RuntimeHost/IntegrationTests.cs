using System;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Models;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace Diagnostics.Tests.Integration.RuntimeHost
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Diagnostics.RuntimeHost.Startup>>
    {
        private readonly WebApplicationFactory<Diagnostics.RuntimeHost.Startup> _factory;

        public IntegrationTests(WebApplicationFactory<Diagnostics.RuntimeHost.Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/healthping")]
        public async Task HealthPingTest(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/subscriptions/ef90e930-9d7f-4a60-8a99-748e0eea69de/resourceGroups/detector-development/providers/Microsoft.Web/sites/buggybakery/detectors")]
        public async Task ListDetectorsTest(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            // Act
            string responseString = await response.Content.ReadAsStringAsync();
            var detectors = JsonConvert.DeserializeObject<DiagnosticApiResponse[]>(responseString);

            // Assert
            Assert.True(detectors.Length > 5);
        }

        [Theory]
        [InlineData("/subscriptions/ef90e930-9d7f-4a60-8a99-748e0eea69de/resourceGroups/detector-development/providers/Microsoft.Web/sites/buggybakery/detectors/httpservererrors")]
        public async Task CallDetectorTest(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            // Act
            string responseString = await response.Content.ReadAsStringAsync();
            var detector = JsonConvert.DeserializeObject<DiagnosticApiResponse>(responseString);

            // Assert
            Assert.True(detector != null);
            Assert.True(detector.Dataset.Count >= 4);
        }

        private async Task LaunchCompilerHostProcess(string requestId)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _compilerHostBinaryLocation,
                    FileName = "dotnet",
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

        [Theory]
        [InlineData("/subscriptions/ef90e930-9d7f-4a60-8a99-748e0eea69de/resourceGroups/detector-development/providers/Microsoft.Web/sites/buggybakery/detectors/httpservererrors")]
        public async Task CompileTest(string url)
        {
            // Start Compiler Host



            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());

            // Act
            string responseString = await response.Content.ReadAsStringAsync();
            var detector = JsonConvert.DeserializeObject<DiagnosticApiResponse>(responseString);

            // Assert
            Assert.True(detector != null);
            Assert.True(detector.Dataset.Count >= 4);
        }
    }
}
