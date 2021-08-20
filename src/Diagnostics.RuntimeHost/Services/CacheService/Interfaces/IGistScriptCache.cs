using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Diagnostics.RuntimeHost.Services.CacheService.Interfaces
{
    public interface IGistScriptCache: ICache<string, string>
    {
    }
}
