using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class Category
    {
        public string Name;
        public DateTime Period;
        public int CasesLeaked;
        public int CasesDeflected;
        public double DeflectionPercentage;
        public double ChangesFromLastPeriod;
    }
}
