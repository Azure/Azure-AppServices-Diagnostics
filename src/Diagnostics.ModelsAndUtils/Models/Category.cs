using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class Category 
    {
        private Category(string categoryName)
        {
            Value = categoryName;
        }

        public string Value { get; private set; }

        public static Category Custom(string name)
        {
            return new Category(name);
        }

        public static readonly Category AvailabilityAndPerformance = new Category("Availability and Performance");
        public static readonly Category ConfigurationAndManagement = new Category("Configuration and Management");
    }
}
