using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Enum of Action Type
    /// </summary>
    public enum SummaryCardActionType
    {
        Detector,
        Tool
    }

    /// <summary>
    /// Enum of Summary Card Status
    /// </summary>
    public enum SummaryCardStatus
    {
        Critical,
        Warning,
        Info,
        Success,
        None
    }

    public class SummaryCard
    {
        /// <summary>
        /// Title of Summary card,shown in top of card
        /// </summary>
        public string Title { set; get; }

        /// <summary>
        /// Message of Summary card,shown in middle of card
        /// </summary>
        public string Message { set; get; }

        /// <summary>
        /// Spicfy the status(Critical,Warning,Info,Success,None) shown as icon in middle left card
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SummaryCardStatus Status { set; get; }

        /// <summary>
        /// Description of Summary card,shown in bottom card
        /// </summary>
        public string Description { set; get; }

        /// <summary>
        /// Spicfy the Action Type(Detector,Tool)
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SummaryCardActionType ActionType { set; get; }

        /// <summary>
        /// Spicfy the Link to detector(detectorId) or tool
        /// </summary>
        public string Link { set; get; }

        /// <summary>
        /// Create an instance of Summary Card
        /// </summary>
        /// <param name="status">Status(Critical,Warning,Info,Success,None) shown as icon in middle left card</param>
        /// <param name="title">Title for card,shown in top of card</param>
        /// <param name="message">Message of Summary card,shown in middle of card</param>
        /// <param name="description">Description of Summary card,shown in bottom of card</param>
        /// <param name="link">Link to detector(detectorId) or tool</param>
        /// <param name="actionType">Action Type for card</param>
        public SummaryCard(SummaryCardStatus status, string title,string message,string description,string link,SummaryCardActionType actionType = SummaryCardActionType.Detector) 
        {
            this.Status = status;
            this.Message = message;
            this.Description = description;
            this.Title = title;
            this.Link = link;
            this.ActionType = actionType;
        }
    }


    public static class ResponseSummaryCardExtension
    {
        /// <summary>
        /// Add a list of summary cards in response
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="summaryCards">List<![CDATA[<SummaryCard>]]></param>
        /// <returns></returns>
        /// <example>
        /// This sample code shows how to use <see cref="AddSummaryCards"> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        ///{
        ///     var summaryCards = new List<![CDATA[<SummaryCard>]]>();
        ///         
        ///     summaryCards.Add(
        ///         new SummaryCard(
        ///             title: "Request and Errors",
        ///             description: "HTTP errors",
        ///             status: SummaryCardStatus.Info,
        ///             message: "25",
        ///             link: "http4xx",
        ///             actionType: SummaryCardActionType.Detector));
        ///
        ///     summaryCards.Add(
        ///         new SummaryCard(
        ///             title: "High CPU Performance",
        ///             description: "% CPU usage",
        ///             status: SummaryCardStatus.Critical,
        ///             message: "65%",
        ///             link: "http4xx",
        ///             actionType: SummaryCardActionType.Tool));
        ///
        ///
        ///      res.AddSummaryCards(summaryCards);
        ///}
        /// </code>
        /// </example>
    public static DiagnosticData AddSummaryCards(this Response response,List<SummaryCard> summaryCards)
        {
            if (summaryCards == null || !summaryCards.Any())
            {
                throw new ArgumentException("Paramter List<SummaryCard> is null or contains no elements.");
            }

            var table = new DataTable();
            table.Columns.Add("Status",typeof(string));
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Message",typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Link", typeof(string));
            table.Columns.Add("ActionType", typeof(string));

            foreach (var summaryCard in summaryCards)
            {
                DataRow nr = table.NewRow();
                nr["Status"] = summaryCard.Status;
                nr["Title"] = summaryCard.Title;
                nr["Message"] = summaryCard.Message;
                nr["Description"] = summaryCard.Description;
                nr["Link"] = summaryCard.Link;
                nr["ActionType"] = summaryCard.ActionType;
                table.Rows.Add(nr);
                //table.Rows.Add(summaryCard.Status,summaryCard.Title,summaryCard.Message,summaryCard.Description);
            }

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.SummaryCard)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }

        /// <summary>
        /// Add a single summary card
        /// </summary>
        /// <param name="response">Response</param>
        /// <param name="status">SummaryCardStatus</param>
        /// <param name="title">Title</param>
        /// <param name="message">Message</param>
        /// <param name="description">Description</param>
        /// <param name="link">Link</param>
        /// <param name="actionType">ActionType</param>
        /// <returns></returns>
        /// <example>
        /// This sample code shows how to use <see cref="AddSummaryCard"> method.
        /// <code>
        /// /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        ///{
        ///     res.AddSummaryCard(
        ///         SummaryCardStatus.Info
        ///         "Request and Errors",
        ///         "25",
        ///         "HTTP errors",
        ///         "http4xx",
        ///         SummaryCardActionType.Detector);
        ///}     
        /// </code>
        /// </example>
        public static DiagnosticData AddSummaryCard(this Response response, SummaryCardStatus status,string title,string message,string description,string link,SummaryCardActionType actionType = SummaryCardActionType.Detector)
        {
            var summaryCard = new SummaryCard(status,title,message,description,link,actionType);
            return AddSummaryCards(response, new List<SummaryCard> { summaryCard });
        }
    }
}