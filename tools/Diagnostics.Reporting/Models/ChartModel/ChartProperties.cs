using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class ChartProperties
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public string ChartType { get; set; }

        public RGBColor Color { get; set; }

        public ChartProperties()
        {
            Color = new RGBColor(0, 0, 0);
        }
    }
}
