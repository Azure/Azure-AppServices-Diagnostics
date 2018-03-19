using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders
{
    public interface IDataProviderConfiguration
    {
        void PostInitialize();
    }
}
