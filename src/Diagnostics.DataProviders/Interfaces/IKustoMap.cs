using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.DataProviders.Interfaces
{
    public interface IKustoMap
    {
        string MapCluster(string cluster);
        string MapDatabase(string database);
    }
}
