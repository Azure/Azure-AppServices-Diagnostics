using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class Guage
    {
        /// <summary>
        /// Create a new Guage instance. Does not have a default constructor.
        /// </summary>
        public Guage(InsightStatus status, double percentFilled, string displayValue, string label, GuageSize size = GuageSize.Medium, string description = "")
        {
            this.Status = status;
            this.PercentFilled = percentFilled;
            this.DisplayValue = displayValue;
            this.Label = label;
            this.Size = size;
            this.Description = description;
        }

        /// <summary>
        /// Insight Level for the Guage. Decides the color of the Guage. Red for Critical, Orange for Warning, Green for Success and Blue for Info & None.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InsightStatus Status { get; set; }

        private double _percentFilled;

        /// <summary>
        /// Decides the percentage up to which the Guage should be filled
        /// </summary>
        public double PercentFilled
        {
            get { return _percentFilled; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Supplied value for PercentFilled should be a non negetive number");

                if (value > 100)
                    throw new ArgumentException("Supplied value for PercentFilled should be less than or equal to 100");

                _percentFilled = value;
            }
        }

        /// <summary>
        /// Text to show within the Guage, typically a number representation. Can be a markdown string. No need to decorate the string with the <markdown> tag
        /// </summary>
        public string DisplayValue { get; set; }

        /// <summary>
        /// Text to show under the Guage, typically describes the value represented by the Guage. Can be a markdown string. No need to decorate the string with the <markdown> tag
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Size of the Guage. Can be either Small, Medium or Large
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GuageSize Size { get; set; }

        /// <summary>
        /// Any additional information that needs to be shown to describe the value and its significance in detail. This appears to the right of the guage. Can be a markdown string. No need to decorate the string with the <markdown> tag
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// The direction in which to render multiple guages. To be used only with the Response.AddGuages extension method.
    /// Setting the rendering direction to Horizontal will cause the Description property of the guage to be ignored during rendering.
    /// </summary>
    public enum GuageRenderDirection
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Size of the Guage to Render.
    /// </summary>
    public enum GuageSize
    {
        Small,
        Medium,
        Large
    }

    public static class ResponseGuageExtension
    {
        /// <summary>
        /// Adds multiple Guage's to Response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="guages">List<![CDATA[<Guage>]]></param>
        /// <param name="renderDirection">GuageRenderDirection</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddGuages"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     List<Guage> guages = new List<Guage>();
        ///     for(int i = 0; i &lt; 15; i++)
        ///     {
        ///         double util = (i + 1) * 3.0;
        ///         if(util == 15)
        ///         {
        ///             string markdownString = $@"
        ///| | |
        ///|-|-|
        ///|__Counter Name__| Value1
        ///|__Counter Value__| Value2
        ///|__Description__| Value2
        ///|__More Info__| Value2
        ///";
        ///             Guage g = new Guage(InsightStatus.Warning, util, "`" + util.ToString() + " %`", "`" + util.ToString() + "% Utilized`", GuageSize.Medium, markdownString);
        ///             guages.Add(g);
        ///         }
        ///         else
        ///         {
        ///             if(util == 21)
        ///             {
        ///                 Guage g = new Guage(InsightStatus.Critical, util, "`" + util.ToString() + " %`", "`" + util.ToString() + "% Utilized`", GuageSize.Medium, "`Some markdown string`");
        ///                 guages.Add(g);
        ///             }
        ///             else
        ///             {
        ///                 Guage g = new Guage(InsightStatus.Info, util, "`" + util.ToString() + " %`", "`" + util.ToString() + "% Utilized`", GuageSize.Small, "`Some markdown string`");
        ///                 guages.Add(g);
        ///             }
        ///         }
        ///     }
        ///     res.AddGuages(guages, GuageRenderDirection.Horizontal);
        ///     res.AddInsight(InsightStatus.Info, "More detailed info below");
        ///     res.AddGuages(guages, GuageRenderDirection.Vertical);
        ///     return res;
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddGuages(this Response response, List<Guage> guages, GuageRenderDirection renderDirection)
        {
            if (guages == null || !guages.Any())
                throw new ArgumentNullException("Paramter List<Guage> is null or contains no elements.");

            var table = new DataTable();
            table.Columns.Add("RenderDirection", typeof(int));
            table.Columns.Add("Size", typeof(int));
            table.Columns.Add("FillColor", typeof(int));
            table.Columns.Add("PercentFilled", typeof(double));
            table.Columns.Add("DisplayValue", typeof(string));
            table.Columns.Add("Label", typeof(string));
            table.Columns.Add("Description", typeof(string));

            foreach (Guage g in guages)
            {
                table.Rows.Add(
                    JsonSerializer.Serialize(renderDirection),
                    JsonSerializer.Serialize(g.Size),
                    g.Status,
                    g.PercentFilled,
                    g.DisplayValue,
                    g.Label,
                    g.Description
                    );
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Guage)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }

        /// <summary>
        /// Adds a Guage to Response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="guage">Guage</param>
        /// <returns></returns>
        /// <example>
        /// This sample shows how to use <see cref="AddGuage"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     Guage g = new Guage(InsightStatus.Warning, 80.0, "80 %", "`80% Utilized`", GuageSize.Large, "`Some markdown string`");
        ///     res.AddGuage(g);
        ///     return res;
        ///}
        /// </code>
        /// </example>
        public static DiagnosticData AddGuage(this Response response, Guage guage)
        {
            if (guage == null) return null;
            return AddGuages(response, new List<Guage>() { guage }, GuageRenderDirection.Vertical);
        }

        public static DiagnosticData AddGuage(this Response response, InsightStatus status, double percentFilled, string displayValue, string label, GuageSize size = GuageSize.Medium, string description = "")
        {
            Guage guage = new Guage(status, percentFilled, displayValue, label, size, description);
            return AddGuages(response, new List<Guage>() { guage }, GuageRenderDirection.Vertical);
        }
    }
}
