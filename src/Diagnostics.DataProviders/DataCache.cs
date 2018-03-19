using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class OperationDataCache
    {
        private Lazy<ConcurrentDictionary<string, CacheMember>> _cacheInitialization = new Lazy<ConcurrentDictionary<string, CacheMember>>(() => new ConcurrentDictionary<string, CacheMember>());

        private ConcurrentDictionary<string, CacheMember> _cache
        {
            get
            {
                return _cacheInitialization.Value;
            }
        }

        public Task<dynamic> GetOrAdd(string key, Func<string, CacheMember> cacheMember)
        {
            return _cache.GetOrAdd(key, cacheMember).DataTask;
        }
    }

    public class CacheMember
    {
        public Task<dynamic> DataTask { get; set; }

        public string MetaData { get; set; }
    }
}
