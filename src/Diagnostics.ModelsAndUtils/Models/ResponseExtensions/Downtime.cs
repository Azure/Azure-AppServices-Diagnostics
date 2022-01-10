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
        public DateTime StartTime { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        /// <summary>
        /// The end time for the downtime period
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        /// <summary>
        /// A optional label that if specified can be used to render a label or span in downtime analysis
        /// </summary>
        public string DownTimeLabel { get; set; }

        /// <summary>
        /// A boolean flag indicating whether this should be preferred downtime to be pre-selected
        /// </summary>
        public bool IsSelected { get; set; }
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
            table.Columns.Add("IsSelected", typeof(bool));

            foreach (var d in downtimes)
            {
                table.Rows.Add(
                    d.StartTime, 
                    d.EndTime, 
                    d.DownTimeLabel,
                    d.IsSelected);
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
