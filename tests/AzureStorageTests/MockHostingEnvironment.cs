using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Diagnostics.Tests.AzureStorageTests
{
    internal class MockHostingEnvironment: IHostingEnvironment
    {
        public MockHostingEnvironment()
        {

        }
        public string EnvironmentName { get; set; }
        public string ApplicationName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string WebRootPath { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string ContentRootPath { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    }
}