using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public DataTable Table { get; set; }

        public Rendering RenderingProperties { get; set; }

        public DiagnosticData()
        {
            Table = new DataTable();
            RenderingProperties = new Rendering();
        }
    }

    public class DiagnosticApiResponse
    {
        public Definition Metadata { get; set; }

        public List<DiagnosticDataApiResponse> Dataset { get; set; }

        public static DiagnosticApiResponse FromCsxResponse(Response response)
        {
            return new DiagnosticApiResponse()
            {
                Metadata = response.Metadata,
                Dataset = response.Dataset.Select(dataSet =>
                    new DiagnosticDataApiResponse()
                    {
                        RenderingProperties = dataSet.RenderingProperties,
                        Table = DataTableUtility.GetDataTableResponseObject(dataSet.Table)
                    }).ToList()
            };
        }

        public DiagnosticApiResponse()
        {
            Metadata = new Definition();
            Dataset = new List<DiagnosticDataApiResponse>();
        }
    }

    public class DiagnosticDataApiResponse
    {
        public DataTableResponseObject Table { get; set; }

        public Rendering RenderingProperties { get; set; }

        public DiagnosticDataApiResponse()
        {
            Table = new DataTableResponseObject();
            RenderingProperties = new Rendering();
        }
    }
}
