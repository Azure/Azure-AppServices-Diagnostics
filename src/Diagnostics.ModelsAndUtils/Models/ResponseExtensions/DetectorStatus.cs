using System;
using System.Data;
using System.Linq;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorStatusExtensions
    {
        /// <summary>
        /// Sets the status of a detector
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="health">Detector Status</param>
        /// <param name="message">Detector status message</param>
        /// <example>
        /// This sample shows how to use <see cref="SetDetectorStatus"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     res.SetDetectorStatus(InsightStatus.Critical, "This is an error message that will come from the detector");
        /// }
        /// </code>
        /// </example>
        public static void SetDetectorStatus(this Response response, InsightStatus status, string message = null)
        {
            response.Status = new Status()
            {
                StatusId = status,
                Message = message
            };
        }

        public static void UpdateDetectorStatusFromInsights(this Response response)
        {
            if (response.Status != null)
            {
                return;
            }

            var insights = response.Dataset
                .Where(set => set.RenderingProperties.Type == RenderingType.Insights || set.RenderingProperties.Type == RenderingType.DynamicInsight);

            if (!insights.Any())
            {
                response.Status = new Status() { StatusId = InsightStatus.None };
                return;
            }

            var status = insights.Select(set =>
                {
                    if (set.RenderingProperties.Type == RenderingType.DynamicInsight)
                    {
                        return ((DynamicInsightRendering)set.RenderingProperties).Status;
                    }
                    else
                    {
                        int lowestStatus = (int)InsightStatus.None;
                        foreach (DataRow row in set.Table.Rows)
                        {
                            var insightStatusInt = (int)Enum.Parse(typeof(InsightStatus), row["Status"].ToString());
                            if (insightStatusInt < lowestStatus)
                            {
                                lowestStatus = insightStatusInt;
                            }
                        }
                        return (InsightStatus)lowestStatus;
                    }
                }).OrderBy(st => st).FirstOrDefault();

            response.Status = new Status() { StatusId = status };
        }
    }
}
