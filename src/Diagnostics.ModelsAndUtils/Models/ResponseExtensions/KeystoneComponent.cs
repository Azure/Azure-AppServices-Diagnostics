using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class KeystoneInsight
    {
        public InsightStatus Status;
        public string Title;
        public string Description;
        public Solution Solution;
        public DateTime StartDate;
        public DateTime ExpiryDate;

        public KeystoneInsight(InsightStatus status, string title, string description, Solution solution=null, DateTime? startDate= null, DateTime? expiryDate = null)
        {
            Status = status;
            Title = title;
            Description = description;
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
                if (keystoneInsight.Status == null || string.IsNullOrWhiteSpace(keystoneInsight.Title) || string.IsNullOrWhiteSpace(keystoneInsight.Description))
                {
                    throw new Exception("Required attributes Status, Title, and Description cannot be null or empty for KeystoneInsight.");
                }
                if (DateTime.Compare(keystoneInsight.ExpiryDate, DateTime.UtcNow) <= 0)
                {
                    throw new Exception("Invalid ExpiryDate, ExpiryDate should be greater than current UTC datetime.");
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
