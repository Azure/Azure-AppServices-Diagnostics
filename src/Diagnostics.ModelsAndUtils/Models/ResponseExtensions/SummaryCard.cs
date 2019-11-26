using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public enum SummaryCardActionType
    {
        Detector,
        Tool
    }

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
        public string Title { set; get; }

        public string Message { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SummaryCardStatus Status { set; get; }

        public string Description { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SummaryCardActionType ActionType { set; get; }

        public string Link { set; get; }

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

        public static DiagnosticData AddSummaryCard(this Response response, SummaryCardStatus status,string title,string message,string description,string link,SummaryCardActionType actionType = SummaryCardActionType.Detector)
        {
            var summaryCard = new SummaryCard(status,title,message,description,link,actionType);
            return AddSummaryCards(response, new List<SummaryCard> { summaryCard });
        }
    }
}