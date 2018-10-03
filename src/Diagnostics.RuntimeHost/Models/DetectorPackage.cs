using Diagnostics.RuntimeHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    public class DetectorPackage
    {
        private string _sanitizedCodeString;

        public string CodeString
        {
            get
            {
                return _sanitizedCodeString;
            }
            set
            {
                _sanitizedCodeString = FileHelper.SanitizeScriptFile(value);
            }
        }

        public string DllBytes;

        public string PdbBytes;

        public string Id;

        public string CommittedByAlias;
    }
}
