using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Data Summary
    /// </summary>
    public class DataSummary
    {
        /// <summary>
        /// Name/Title of the Summary Object.
        /// </summary>
        public string Name;

        /// <summary>
        /// Value
        /// </summary>
        public string Value;

        /// <summary>
        /// Color (RGB string eg:- "#7eb100" or "blue")
        /// </summary>
        public string Color;

        /// <summary>
        /// Creates an instance of DataSummary class.
        /// </summary>
        /// <param name="name">Name of the summary object</param>
        /// <param name="value">Value of the summary object</param>
        public DataSummary(string name, string value)
        {
            this.Name = name ?? string.Empty; ;
            this.Value = value ?? string.Empty;
            this.Color = string.Empty;
        }

        /// <summary>
        /// Creates an instance of DataSummary class.
        /// </summary>
        /// <param name="name">Name of the summary object</param>
        /// <param name="value">Value of the summary object</param>
        /// <param name="color">Color</param>
        public DataSummary(string name, string value, string color)
        {
            this.Name = name ?? string.Empty; ;
            this.Value = value ?? string.Empty;
            this.Color = color ?? string.Empty;
        }
    }

    public static class ResponseDataSummaryExtension
    {
        /// <summary>
        /// Adds a list of <see cref="DataSummary"/> points to the response.
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="dataSummaryPoints">List of DataSummary points</param>
        /// <param name="title">Title of summary</param>
        /// <param name="description">Description of Summary</param>
        /// <example>
        /// This sample shows how to use <see cref="AddDataSummary"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     DataSummary ds1 = new DataSummary("Title1", "40");
        ///     DataSummary ds2 = new DataSummary("Title2", "60", "red");
        ///
        ///     res.AddDataSummary(new List<![CDATA[<DataSummary>]]>(){ ds1, ds2 }, "Summary Title", "This is an optional summary description");
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddDataSummary(this Response response, List<DataSummary> dataSummaryPoints, string title = null, string description = null)
        {
            if (dataSummaryPoints == null || !dataSummaryPoints.Any())
            {
                return null;
            }

            DataTable table = new DataTable("Data Summary");

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Name", typeof(string)),
                new DataColumn("Value", typeof(string)),
                new DataColumn("Color", typeof(string))
            });

            dataSummaryPoints.ForEach(item =>
            {
                table.Rows.Add(new string[]
                {
                    item.Name,
                    item.Value,
                    item.Color
                });
            });

            var summaryData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.DataSummary)
                {
                    Title = title,
                    Description = description
                }
            };

            response.Dataset.Add(summaryData);

            return summaryData;
        }
    }
}
