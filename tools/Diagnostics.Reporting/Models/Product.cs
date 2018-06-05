using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class Product
    {
        public string ProductId;
        public string ProductName;
        public DateTime Period;
        public int CasesLeaked;
        public int CasesDeflected;
        public double DeflectionPercentage;
        public double ChangesFromLastPeriod;
        public string P360Link
        {
            get
            {
                return Helper.GetP360Link(ProductId);
            }
        }
        public List<Category> Categories;
    }
}
