using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Dropdown
    {
        /// <summary>
        /// Represents the label shown for dropdown selector
        /// </summary>
        public string Label;

        /// <summary>
        /// Tuple representing Data. 
        /// First Item represents item in Dropdown, Second Item represents whether selected by default in dropdwon, Third Item represents the body after dropdown selection
        /// </summary>
        public List<Tuple<string, bool, Response>> Data;

        /// <summary>
        /// Creates an instance of Dropdown Class
        /// </summary>
        /// <param name="label">Dropdown Label</param>
        /// <param name="data">Dropdown Data</param>
        public Dropdown(string label, List<Tuple<string, bool, Response>> data)
        {
            this.Label = label;
            this.Data = data;
        }
    }

    public static class ResponseDropdownExtension
    {
        public static DiagnosticData AddDropdownView(this Response response, Dropdown dropdownView, string title = null)
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
                RenderingProperties = new Rendering(RenderingType.DropDown)
                {
                    Title = title ?? string.Empty
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
