using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ISupportObserverDataProvider
    {
        Task<dynamic> GetSite(string siteName);
        Task<dynamic> GetSite(string stampName, string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string siteName);
        Task<Dictionary<string, List<RuntimeSitenameTimeRange>>> GetRuntimeSiteSlotMap(string stampName, string siteName);
        Task<dynamic> GetResource(string wawsObserverUrl);
    }
}
