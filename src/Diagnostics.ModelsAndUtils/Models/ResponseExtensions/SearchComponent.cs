using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class DetectorSearchConfiguration
    {
        public double MinScoreThreshold { get; set; }
        public int MaxResults { get; set; }

        public DetectorSearchConfiguration(double minScoreThreshold=0.3, int maxResults = 10)
        {
            MinScoreThreshold = minScoreThreshold;
            MaxResults = maxResults;
        }       
    }

    public class WebSearchConfiguration
    {
        public int MaxResults { get; set; }
        public bool UseStack { get; set; }
        public List<string> PreferredSites { get; set; }
        public WebSearchConfiguration(int maxResults = 5, bool useStack=true, List<string> preferredSites = null)
        {
            MaxResults = maxResults;
            UseStack = useStack;
            PreferredSites = preferredSites!=null? preferredSites: new List<string>();
        }
    }
    public static class ResponseSearchComponentExtension
    {
        public static DiagnosticData AddSearch(this Response response)
        {
            var table = new DataTable();
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.SearchComponent)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }
        public static DiagnosticData AddSearch(this Response response, bool detectorSearchEnabled=true, bool webSearchEnabled=true, DetectorSearchConfiguration detectorSearchConfiguration=null, WebSearchConfiguration webSearchConfiguration=null, string customQueryString=null)
        {
            var table = new DataTable();
            table.Columns.Add("DetectorSearchEnabled", typeof(bool));
            table.Columns.Add("WebSearchEnabled", typeof(bool));
            table.Columns.Add("DetectorSearchConfiguration", typeof(string));
            table.Columns.Add("WebSearchConfiguration", typeof(string));
            table.Columns.Add("CustomQueryString", typeof(string));
            DataRow newRow = table.NewRow();
            newRow["DetectorSearchEnabled"] = detectorSearchEnabled;
            newRow["WebSearchEnabled"] = webSearchEnabled;
            newRow["DetectorSearchConfiguration"] = detectorSearchConfiguration != null? JsonConvert.SerializeObject(detectorSearchConfiguration): JsonConvert.SerializeObject(new DetectorSearchConfiguration());
            newRow["WebSearchConfiguration"] = webSearchConfiguration != null? JsonConvert.SerializeObject(webSearchConfiguration): JsonConvert.SerializeObject(new WebSearchConfiguration());
            newRow["CustomQueryString"] = customQueryString;
            table.Rows.Add(newRow);
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.SearchComponent)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
