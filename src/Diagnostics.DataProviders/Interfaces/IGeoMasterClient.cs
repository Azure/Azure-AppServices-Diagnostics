using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Diagnostics.DataProviders
{
    interface IGeoMasterClient
    {
        HttpClient Client { get; }
        Uri BaseUri { get; }
    }
}
