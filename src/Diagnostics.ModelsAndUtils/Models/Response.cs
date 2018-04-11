using Diagnostics.ModelsAndUtils.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Response Object
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Metadata of detector.
        /// </summary>
        public Definition Metadata { get; set; }

        /// <summary>
        /// List of DiagnosticData, each of which has specific rendering type and data.
        /// </summary>
        public List<DiagnosticData> Dataset { get; set; }

        /// <summary>
        /// Creates an instance of Response
        /// </summary>
        public Response()
        {
            Metadata = new Definition();
            Dataset = new List<DiagnosticData>();
        }
    }

    public class DiagnosticData
    {
        public DataTable Table { get; set; }

        /// <summary>
        /// Rendering Properties for the Diagnostics Data
        /// </summary>
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
                        Table = dataSet.Table.ToDataTableResponseObject()
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
