using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class KeystoneInsight
    {
        public string Title;
        public string Summary;
        public Solution Solution;
        public DateTime StartDate;
        public DateTime ExpiryDate;

        public KeystoneInsight(string title, string summary, Solution solution=null, DateTime? startDate= null, DateTime? expiryDate = null)
        {
            Title = title;
            Summary = summary;
            Solution = solution;
            StartDate = startDate ?? DateTime.UtcNow;
            ExpiryDate = expiryDate ?? DateTime.MaxValue;
        }
    }

    public static class ResponseKeystoneComponentExtension
    {
        public static DiagnosticData AddKeystoneComponent(this Response response, KeystoneInsight keystoneInsight)
        {
            try
            {
                if (!(keystoneInsight.Title != null && keystoneInsight.Summary != null))
                {
                    throw new Exception("Required attributes Title, and Summary cannot be null for KeystoneInsight.");
                }
                var table = new DataTable();
                table.Columns.Add("Content", typeof(string));
                DataRow newRow = table.NewRow();
                newRow["Content"] = JsonConvert.SerializeObject(keystoneInsight);
                table.Rows.Add(newRow);
                var diagData = new DiagnosticData()
                {
                    Table = table,
                    RenderingProperties = new Rendering(RenderingType.KeystoneComponent)
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
