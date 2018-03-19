using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class Response
    {
        public Definition Metadata { get; set; }

        public List<DiagnosticData> Dataset { get; set; }

        public Response()
        {
            Metadata = new Definition();
            Dataset = new List<DiagnosticData>();
        }
    }

    public class DiagnosticData
    {
        public DataTableResponseObject Table { get; set; }

        public Rendering RenderingProperties { get; set; }

        public DiagnosticData()
        {
            Table = new DataTableResponseObject();
            RenderingProperties = new Rendering();
        }
    }

    public class Rendering
    {
        public GraphType Type { get; set; }

        public Rendering()
        {
            Type = GraphType.TimeSeries;
        }

        public Rendering(GraphType type)
        {
            Type = type;
        }
    }

    public enum GraphType
    {
        NoGraph = 0,
        Table,
        TimeSeries
    }
}
