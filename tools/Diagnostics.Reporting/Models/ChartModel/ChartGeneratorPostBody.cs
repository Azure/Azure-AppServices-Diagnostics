using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Reporting.Models
{
    public class ChartGeneratorPostBody
    {
        public Axis XAxis;

        public Axis YAxis;

        public ChartProperties Chart;

        public ChartGeneratorPostBody()
        {
            XAxis = new Axis();
            YAxis = new Axis();
            Chart = new ChartProperties();
        }
    }
}
