using System;
using System.Collections.Generic;
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
        public DataTableResponseObject Table { get; set; }

        /// <summary>
        /// Rendering Properties for the Diagnostics Data
        /// </summary>
        public Rendering RenderingProperties { get; set; }

        public DiagnosticData()
        {
            Table = new DataTableResponseObject();
            RenderingProperties = new Rendering();
        }
    }
}
