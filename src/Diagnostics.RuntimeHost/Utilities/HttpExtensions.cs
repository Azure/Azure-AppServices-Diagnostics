using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Diagnostics.RuntimeHost.Utilities
{
    public static class HttpExtensions
    {
        public static async Task<T> ReadAsAsyncCustom<T>(this HttpContent value)
        {
            string responseString = await value.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseString);
        }
    }
}
