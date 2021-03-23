using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Section
    {
        /// <summary>
        /// Section Title
        /// </summary>
        public string Title;

        /// <summary>
        /// Response rendered under this section
        /// </summary>
        public Response Data;

        /// <summary>
        /// Whether section is expaned at beginning
        /// </summary>
        public bool IsExpanded;

        /// <summary>
        /// create an instance of Section class
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="title">Section Title</param>
        /// <param name="isExpanded">Default expand state of section</param>
        public Section(Response data, string title = "", bool isExpanded = true)
        {
            Title = title;
            Data = data;
            IsExpanded = isExpanded;
        }
    }

    public static class ResponseSectionExtension
    {
        /// <summary>
        /// Add a list of section
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="sections">List<![CDATA[<Section>]]></param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddSections"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var sections = new List<![CDATA[<Section>]]>();
        ///     sections.Add(res1, "Section Title - 1", true);
        ///     sections.Add(res2, "Section Title - 2", false);
        ///     res.AddSections(sections);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddSections(this Response response, List<Section> sections)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Title", typeof(string)));
            table.Columns.Add(new DataColumn("Value", typeof(string)));
            table.Columns.Add(new DataColumn("Expanded", typeof(string)));
            foreach (var section in sections)
            {
                List<DiagnosticDataApiResponse> dataSet = section.Data.Dataset.Select(entry =>
                    new DiagnosticDataApiResponse()
                    {
                        RenderingProperties = entry.RenderingProperties,
                        Table = entry.Table.ToDataTableResponseObject()
                    }).ToList();
                table.Rows.Add(new object[]
                {
                    section.Title ?? string.Empty,
                    JsonConvert.SerializeObject(dataSet, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }),
                    section.IsExpanded.ToString()
                }); ;
            }
            
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Section)

            };


            response.Dataset.Add(diagData);
            return diagData;
        }

        /// <summary>
        /// Add one section
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="section">Section</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddSection(Response, Section)"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var section = new Section(res1, "Section Title - 1", true);
        ///     res.AddSection(section);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddSection(this Response response, Section section)
        {
            return AddSections(response, new List<Section> { section });
        }

        /// <summary>
        /// Add one section
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="data">Response under this section</param>
        /// <param name="title">section title</param>
        /// <param name="isExpand">whether section is expanded in beginning</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddSection(Response, Response, string, bool)"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     res.AddSection(res1, "Section Title - 1", true);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddSection(this Response response, Response data, string title = "", bool isExpand = true)
        {
            return AddSection(response, new Section(data,title,isExpand));
        }
    }
}
