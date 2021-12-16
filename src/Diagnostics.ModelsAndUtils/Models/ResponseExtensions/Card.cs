using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public string Title { get; set; }

        /// <summary>
        /// A list of descriptions for this card
        /// </summary>
        public List<string> Descriptions { get; set; }

        /// <summary>
        /// Specify and icon from the font-awesome collection (for e.g. fa-circle)
        /// </summary>
        public string Icon;

        /// <summary>
        /// Specify the action type for this card
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CardActionType ActionType { get; set; }

        /// <summary>
        /// Specify the action value for the card (will be detectorId for detectors)
        /// </summary>
        public string ActionValue { get; set; }

        /// <summary>
        /// Creates an instance of Card class.
        /// </summary>
        /// <param name="title">Title text for the card</param>
        /// <param name="descriptions">List of descriptions to add to the card.</param>
        /// <param name="icon">One of the icons from the font-awesome collection (for e.g. fa-circle)</param>
        /// <param name="actionValue">Action value for the card (will be detectorId for detectors)</param>
        /// <param name="actionType">One of the supported ActionType</param>
        public Card(string title, List<string> descriptions, string icon, string actionValue, CardActionType actionType = CardActionType.Detector)
        {
            Title = title;
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
        ///                     descriptions: new List<![CDATA[<string>]]> { "Getting 401, 405, 403 error", "The web app is stopped", "You are not authorized to view this page" },
        ///                     icon: "fa-circle",
        ///                     actionType: CardActionType.Detector,
        ///                     actionValue: "http4xx"));
        ///
        ///     cards.Add(new Card(
        ///                     title: "HTTP Server Errors",
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
            table.Columns.Add(new DataColumn("Descriptions", typeof(string)));
            table.Columns.Add(new DataColumn("ActionType", typeof(string)));
            table.Columns.Add(new DataColumn("ActionValue", typeof(string)));

            cards.ForEach(card =>
            {
                table.Rows.Add(new object[] {
                    card.Title,
                    card.Icon,
                    JsonSerializer.Serialize(card.Descriptions),
                    JsonSerializer.Serialize(card.ActionType),
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
