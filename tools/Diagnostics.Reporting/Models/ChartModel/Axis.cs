using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class Axis
    {
        /// <summary>
        /// Title of the Axis
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the data (string, int, double ...)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Data Values
        /// </summary>
        public List<object> Values { get; set; }

        public Axis()
        {
            Values = new List<object>();
        }
    }
}
