using System.Net.Http;

namespace Diagnostics.Tests.Helpers
{
    internal class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
