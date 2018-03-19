﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    public interface ISourceWatcher
    {
        void Start();

        Task WaitForFirstCompletion();
    }
}
