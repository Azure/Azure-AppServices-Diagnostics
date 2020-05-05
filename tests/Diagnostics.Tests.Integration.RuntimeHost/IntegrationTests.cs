using System;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;

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
            WebApplicationFactoryClientOptions options = new WebApplicationFactoryClientOptions();
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }
    }
}
