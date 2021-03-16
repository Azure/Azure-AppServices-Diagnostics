using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Tab
    {
        /// <summary>
        /// Label of the Tab
        /// </summary>
        public string Label;

        /// <summary>
        /// Item represents the body
        /// </summary>
        public Response Data;

        /// <summary>
        /// True if the item count is shown with the title (e.g. Title(10))
        /// </summary>
        public bool ShowItemCount;

        /// <summary>
        /// The item count shown with the title (e.g. Title(10))
        /// </summary>
        public int ItemCountValue;

        /// <summary>
        /// Specify and icon from the font-awesome collection (for e.g. fa-circle)
        /// </summary>
        public string Icon;

        /// <summary>
        /// Specify whether the tab has a small red dot next to the title
        /// </summary>
        public bool NeedsAttention;

        /// <summary>
        /// Creates an instance of Tab class.
        /// </summary>
        /// <param name="label">Label text for the tab</param>
        /// <param name="needsAttention">True if the tab has a small red dot next to the title</param>
        public Tab(string label, Response data, bool needsAttention = false)
        {
            Label = label;
            Data = data;
            NeedsAttention = needsAttention;
        }

        /// <summary>
        /// Creates an instance of Tab class.
        /// </summary>
        /// <param name="label">Label text for the tab</param>
        /// <param name="icon">Name of the icon found at https://developer.microsoft.com/en-us/fluentui#/styles/web/icons#fabric-react </param>
        /// <param name="needsAttention">True if the tab has a small red dot next to the title</param>
        public Tab(string label, Response data, string icon, bool needsAttention = false)
        {
            Label = label;
            Data = data;
            Icon = icon;
            NeedsAttention = needsAttention;
        }

        /// <summary>
        /// Creates an instance of Tab class.
        /// </summary>
        /// <param name="label">Label text for the tab</param>
        /// <param name="icon">Name of the icon found at https://developer.microsoft.com/en-us/fluentui#/styles/web/icons#fabric-react </param>
        /// <param name="itemCount">The item count shown with the title (e.g. Title(10))</param>
        /// <param name="needsAttention">True if the tab has a small red dot next to the title</param>
        public Tab(string label, Response data, string icon, int itemCount, bool needsAttention = false)
        {
            Label = label;
            Data = data;
            Icon = icon;
            ShowItemCount = true;
            ItemCountValue = itemCount;
            NeedsAttention = needsAttention;
        }
    }

    public static class ResponseTabExtension
    {
        /// <summary>
        /// Adds a list of Cards to Response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="tabs">List<![CDATA[<Tab>]]></param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddTabs"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///
        ///     var tabs = new List<![CDATA[<Tab>]]>();
        ///
        ///     tabs.Add(new Tab(
        ///                     label: "Application Logs",
        ///                     data: res1,
        ///                     icon: "Questionnaire",
        ///                     itemCount: 100,
        ///                     needsAttention: true));
        ///
        ///     tabs.Add(new Tab(
        ///                     label: "Platform Logs",
        ///                     data: res2));
        ///
        ///     res.AddTabs(tabs);
        ///}
        /// </code>
        /// </example>
        public static DiagnosticData AddTabs(this Response response, List<Tab> tabs, string title = null)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Label", typeof(string)));
            table.Columns.Add(new DataColumn("Icon", typeof(string)));
            table.Columns.Add(new DataColumn("ShowItemCount", typeof(bool)));
            table.Columns.Add(new DataColumn("ItemCountValue", typeof(int)));
            table.Columns.Add(new DataColumn("Value", typeof(string)));
            table.Columns.Add(new DataColumn("NeedsAttention", typeof(bool)));

            foreach (var tab in tabs)
            {
                List<DiagnosticDataApiResponse> dataSet = tab.Data.Dataset.Select(entry =>
                    new DiagnosticDataApiResponse()
                    {
                        RenderingProperties = entry.RenderingProperties,
                        Table = entry.Table.ToDataTableResponseObject()
                    }).ToList();

                table.Rows.Add(new object[] {
                    tab.Label ?? string.Empty,
                    tab.Icon,
                    tab.ShowItemCount,
                    tab.ItemCountValue,
                    JsonConvert.SerializeObject(dataSet, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }),
                    tab.NeedsAttention
                });
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Tab)
                {
                    Title = title ?? string.Empty
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
