using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface ITokenService
    {
        Task<string> GetAuthorizationTokenAsync();
    }
}
