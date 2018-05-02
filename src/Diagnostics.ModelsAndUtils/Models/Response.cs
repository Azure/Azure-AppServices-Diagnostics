using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
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
        /// Health of response
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Creates an instance of Response
        /// </summary>
        public Response()
        {
            Status = new Status();
            Metadata = new Definition();
            Dataset = new List<DiagnosticData>();
        }
    }

    public class Status
    {
        public string Message { get; set; }
        public DetectorStatus StatusId { get; set; }

        public Status()
        {
            StatusId = DetectorStatus.None;
        }
    }

    /// <summary>
    /// Status of Detector
    /// </summary>
    public enum DetectorStatus
    {
        /// <summary>
        /// No status, this is the default
        /// </summary>
        None = 0,

        /// <summary>
        /// The detector analyzed the data and determined there was no issue
        /// </summary>
        Healthy,

        /// <summary>
        /// The detector analyzed the data and determined there is a warning
        /// </summary>
        Warning,

        /// <summary>
        /// The detector analyzed the data and determined there is a critical problem or error
        /// </summary>
        Critical,

        /// <summary>
        /// The detector is merely informational
        /// </summary>
        Info
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

        public Status Status { get; set; }

        public static DiagnosticApiResponse FromCsxResponse(Response response)
        {
            return new DiagnosticApiResponse()
            {
                Metadata = response.Metadata,
                Status = response.Status,
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
            Status = new Status();
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
