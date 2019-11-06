using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class MetaData
    {
        public string Title { set; get; }

        public string Message { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MetaDataStatus Status { set; get; }

        public string Description { set; get; }

        public MetaData(MetaDataStatus status, string title,string message,string description) 
        {
            this.Status = status;
            this.Message = message ?? string.Empty;
            this.Description = description;
            this.Title = title;
        }

    }

    public enum MetaDataStatus
    {
        Critical,
        Warning,
        Info,
        Success,
        None
    }

    public static class ResponseMetaDataExtension
    {
        public static DiagnosticData AddMetaDatas(Response response,List<MetaData> metaDatas)
        {
            if (metaDatas == null || !metaDatas.Any())
            {
                throw new ArgumentException("Paramter List<MetaData> is null or contains no elements.");
            }

            var table = new DataTable();
            table.Columns.Add("Status",typeof(string));
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Message",typeof(string));
            table.Columns.Add("Description", typeof(string));

            foreach (MetaData meta in metaDatas)
            {
                table.Rows.Add(meta.Status,meta.Title,meta.Message,meta.Description);
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.MetaData)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }

        public static DiagnosticData AddMetaData(this Response response,MetaData metaData)
        {
            if (metaData == null)
            {
                return null;
            }
            return AddMetaDatas(response, new List<MetaData> { metaData });
        }

        public static DiagnosticData AddMetaData(this Response response, MetaDataStatus status,string title,string message,string description)
        {
            var metaData = new MetaData(status,title,message,description);
            return AddMetaDatas(response, new List<MetaData> { metaData });
        }
    }
}