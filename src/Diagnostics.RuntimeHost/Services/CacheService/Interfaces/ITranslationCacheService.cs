﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics.RuntimeHost.Models;

namespace Diagnostics.RuntimeHost.Services.CacheService
{
    public interface ITranslationCacheService : ICache<Tuple<string, string>, List<string>> { }
}
