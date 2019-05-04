using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public interface IWawsObserverTokenService
    {
        /// <summary>
        /// Get bearer token.
        /// </summary>
        /// <returns>String <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<string> GetAuthorizationTokenAsync();
    }
}
