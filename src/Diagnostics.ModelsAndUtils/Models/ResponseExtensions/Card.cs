using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public enum CardActionType
    {
        Detector,
        Tool
    }
    public class Card
    {
        /// <summary>
        /// Title of the Card
        /// </summary>
        public string Title;

        /// <summary>
        /// A list of descriptions for this card
        /// </summary>
        public string ShortDescription;

        /// <summary>
        /// A list of descriptions for this card
        /// </summary>
        public List<string> Descriptions;

        /// <summary>
        /// Specify and icon from the font-awesome collection (for e.g. fa-circle)
        /// </summary>
        public string Icon;

        /// <summary>
        /// Specify the action type for this card
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CardActionType ActionType;

        /// <summary>
        /// Specify the action value for the card (will be detectorId for detectors)
        /// </summary>
        public string ActionValue;

        /// <summary>
        /// Creates an instance of Card class.
        /// </summary>
        /// <param name="title">Title text for the card</param>
        /// <param name="shortDescription">Small description text just after the title</param>
        /// <param name="descriptions">List of descriptions to add to the card.</param>
        /// <param name="icon">One of the icons from the font-awesome collection (for e.g. fa-circle)</param>
        /// <param name="actionValue">Action value for the card (will be detectorId for detectors)</param>
        /// <param name="actionType">One of the supported ActionType</param>
        public Card(string title, string shortDescription, List<string> descriptions, string icon, string actionValue, CardActionType actionType = CardActionType.Detector)
        {
            Title = title;
            ShortDescription = shortDescription;
            Descriptions = descriptions;
            Icon = icon;
            ActionType = actionType;
            ActionValue = actionValue;
        }
    }

    public static class ResponseCardExtension
    {
        /// <summary>
        /// Adds a list of Cards to Response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="cards">List<![CDATA[<Card>]]></param>
        /// <returns></returns>
        /// <example> 
        /// This sample shows how to use <see cref="AddCards"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///
        ///     var cards = new List<![CDATA[<Card>]]>();
        ///
        ///     cards.Add(new Card(
        ///                     title: "HTTP 4XX Errors",
        ///                     shortDescription: "Select this category for following issue types",
        ///                     descriptions: new List<![CDATA[<string>]]> { "Getting 401, 405, 403 error", "The web app is stopped", "You are not authorized to view this page" },
        ///                     icon: "fa-circle",
        ///                     actionType: CardActionType.Detector,
        ///                     actionValue: "http4xx"));
        ///     
        ///     cards.Add(new Card(
        ///                     title: "HTTP Server Errors",
        ///                     shortDescription: "Select this category for following issue types",
        ///                     descriptions: new List<![CDATA[<string>]]> { "App failing with 502, 503, 500 error","The Page cannot be displayed","App failing with a server error" },
        ///                     icon: "fa-exclamation",
        ///                     actionType: CardActionType.Detector,
        ///                     actionValue: "httpservererrors"));
        ///
        ///     res.AddCards(cards);
        ///}
        /// </code>
        /// </example>
        public static DiagnosticData AddCards(this Response response, List<Card> cards)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Title", typeof(string)));
            table.Columns.Add(new DataColumn("Icon", typeof(string)));
            table.Columns.Add(new DataColumn("ShortDescription", typeof(string)));
            table.Columns.Add(new DataColumn("Descriptions", typeof(string)));
            table.Columns.Add(new DataColumn("ActionType", typeof(string)));
            table.Columns.Add(new DataColumn("ActionValue", typeof(string)));

            cards.ForEach(card =>
            {
                table.Rows.Add(new object[] {
                    card.Title,
                    card.Icon,
                    card.ShortDescription,
                    JsonConvert.SerializeObject(card.Descriptions),
                    JsonConvert.SerializeObject(card.ActionType),
                    card.ActionValue
              });
            });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Card)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
