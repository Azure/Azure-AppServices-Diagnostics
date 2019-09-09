using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class DownTime
    {
        /// <summary>
        /// Represents the start time for the downtime period
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The end time for the downtime period
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// A optional label that if specified can be used to render a label or span in downtime analysis
        /// </summary>
        public string DownTimeLabel { get; set; }
    }
    public static class ResponseDowntimeExtension
    {
        public static DiagnosticData AddDownTimes(this Response response, IEnumerable<DownTime> downtimes)
        {
            if (downtimes is null)
            {
                throw new ArgumentNullException(nameof(downtimes));
            }

            if (!downtimes.Any())
            {
                throw new ArgumentException("downtimes contains no elements");
            }
            var table = new DataTable();
            table.Columns.Add("StartTime", typeof(DateTime));
            table.Columns.Add("EndTime", typeof(DateTime));
            table.Columns.Add("DownTimeLabel");
 
            foreach (var d in downtimes)
            {
                table.Rows.Add(
                    d.StartTime, 
                    d.EndTime, 
                    d.DownTimeLabel);
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.DownTime)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
