using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class FabDropdown {
        public string Label;
        public List<Tuple<string, bool, Response>> Data;
        public FabDropdown(string label,List<Tuple<string,bool,Response>> data)
        {
            this.Data = data;
            this.Label = label;
        }
    }

    public static class ResponseFabDropdownExtension
    {
        /// <summary>
        /// Adds a Dropdown View to Response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="dropdownView">Dropdown ViewModel</param>
        /// <param name="title">Title</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddDropdownView"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///      string label = "select item here";
        ///      List<![CDATA[<Tuple<string, bool, Response>>]]> data = new List<![CDATA[<Tuple<string, bool, Response>>]]>();
        ///
        ///      string firstDataKey = "key1";
        ///      bool selected = true;
        ///      var firstDataEntry = new Response();
        ///      firstDataEntry.AddMarkdownView(@"some markdown content");
        ///
        ///     data.Add(new Tuple<![CDATA[<string, bool, Response>]]>(firstDataKey, selected, firstDataEntry));
        ///
        ///     Dropdown dropdownViewModel = new Dropdown(label, data);
        ///     res.AddDropdownView(dropdownViewModel);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddDropdownView(this Response response, FabDropdown dropdownView, string title = null)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Label", typeof(string)));
            table.Columns.Add(new DataColumn("Key", typeof(string)));
            table.Columns.Add(new DataColumn("Selected", typeof(bool)));
            table.Columns.Add(new DataColumn("Value", typeof(string)));

            foreach (var item in dropdownView.Data)
            {
                List<DiagnosticDataApiResponse> dataSet = item.Item3.Dataset.Select(entry =>
                    new DiagnosticDataApiResponse()
                    {
                        RenderingProperties = entry.RenderingProperties,
                        Table = entry.Table.ToDataTableResponseObject()
                    }).ToList();

                table.Rows.Add(new object[] {
                    dropdownView.Label,
                    item.Item1,
                    item.Item2,
                    JsonConvert.SerializeObject(dataSet, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    })
                });
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.FabDropdown)
                {
                    Title = title ?? string.Empty
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}