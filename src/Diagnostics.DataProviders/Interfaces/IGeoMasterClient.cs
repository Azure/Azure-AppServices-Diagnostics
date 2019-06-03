using System;
using System.Net.Http;

namespace Diagnostics.DataProviders
{
    internal interface IGeoMasterClient
    {
        HttpClient Client { get; }
        Uri BaseUri { get; }
    }
}
