using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class KeystoneInsightBase
    {
        public string LoggingName;
        public string Title;
        public string Summary;
        public string Details;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    public static class ResponseKeystoneComponentExtension
    {
        public static void AddKeystoneComponent(this Response response, object keystoneInsight)
        {
            try
            {
                KeystoneInsightBase insight = (KeystoneInsightBase)keystoneInsight;
                if (!(insight.LoggingName != null && insight.Title != null && insight.Summary != null))
                {
                    throw new Exception("Required attributes LoggingName, Title, and Summary cannot be null for KeystoneInsight");
                }
                response.KeystoneInsight = insight;
            }
            catch (Exception ex)
            {
                throw new Exception("Keystone Insight validation failed: " + ex.ToString());
            }
        }
    }
}
