using System;
using System.Collections.Generic;
using System.Text;

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
        ///     res.SetDetectorStatus(DetectorStatus.Critical, "This is an error message that will come from the detector");
        /// }
        /// </code>
        /// </example>
        public static void SetDetectorStatus(this Response response, DetectorStatus health, string message = null)
        {
            response.Status = new Status()
            {
                StatusId = health,
                Message = message
            };
        }
    }
}
