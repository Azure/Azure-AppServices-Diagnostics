using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class SubCategory
    {
        public string Name;
        public string CategoryName;
        public string ProductId;
        public string SupportTopicId;
        public DateTime Period;
        public int CasesLeaked;
        public int CasesDeflected;
        public double DeflectionPercentage;
        public double ChangesFromLastPeriod;
    }
}
