using System;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Diagnostics.ModelsAndUtils.Models;

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
        [JsonFileData("TestData.json", "ListDetector")]
        public async Task ListDetectorsTest(string url, int expectedResult)
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
            Assert.True(detectors.Length >= expectedResult);
        }

        [Theory]
        [JsonFileData("TestData.json", "GetDetector")]
        public async Task CallDetectorTest(string url, int expectedResult)
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
            Assert.True(detector.Dataset.Count >= expectedResult);
        }
    }
}
