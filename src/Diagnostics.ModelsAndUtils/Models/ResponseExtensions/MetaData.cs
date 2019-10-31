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
        public MetaData(SummrizeStatus status,string message)
        {
            this.status = status;
            this.message = message;
        }

        public string message { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SummrizeStatus status { set; get; }
    }

    public enum SummrizeStatus
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
            table.Columns.Add("Message",typeof(string));

            foreach (MetaData meta in metaDatas)
            {
                table.Rows.Add(meta.message,meta.status);
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

        public static DiagnosticData AddMetaData(this Response response, SummrizeStatus status,string message)
        {
            var metaData = new MetaData(status, message);
            return AddMetaDatas(response, new List<MetaData> { metaData });
        }
    }
}