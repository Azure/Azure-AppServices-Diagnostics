using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    public class DetectorPackage
    {
        public string CodeString;

        public string DllBytes;

        public string PdbBytes;

        public string Id;

        public string CommittedByAlias;
    }
}
