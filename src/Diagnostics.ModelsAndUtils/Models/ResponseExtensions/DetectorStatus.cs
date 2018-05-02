using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseDetectorStatusExtensions
    {
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
