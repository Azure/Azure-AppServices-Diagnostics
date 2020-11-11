using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseKeystoneComponentExtension
    {
        public static DiagnosticData AddKeystoneComponent(this Response response, object keystoneInsight)
        {
            try
            {
                var loggingName = keystoneInsight.GetType().GetProperty("LoggingName");
                var title = keystoneInsight.GetType().GetProperty("Title");
                var summary = keystoneInsight.GetType().GetProperty("Summary");
                if (!(loggingName.GetValue(keystoneInsight) != null && title.GetValue(keystoneInsight) != null && summary.GetValue(keystoneInsight) != null))
                {
                    throw new Exception("Required attributes LoggingName, Title, and Summary cannot be null for KeystoneInsight");
                }
                var diagData = new DiagnosticData()
                {
                    RenderingProperties = new Rendering(RenderingType.KeystoneComponent) {
                        Description = JsonConvert.SerializeObject(keystoneInsight)
                    }
                };
                response.Dataset.Add(diagData);
                return diagData;
            }
            catch (Exception ex)
            {
                throw new Exception("Keystone Insight validation failed: " + ex.ToString());
            }
        }
    }
}
