using System;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Diagnostics;
using System.Threading.Tasks;

namespace Diagnostics.Tests.Integration.CompilerHost
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Diagnostics.CompilerHost.Startup>>
    {
        private readonly WebApplicationFactory<Diagnostics.CompilerHost.Startup> _factory;

        public IntegrationTests(WebApplicationFactory<Diagnostics.CompilerHost.Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/api/CompilerHost/healthping")]
        public async Task HealthPingTest(string url)
        {
            WebApplicationFactoryClientOptions options = new WebApplicationFactoryClientOptions();
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("text/plain; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }
    }
}