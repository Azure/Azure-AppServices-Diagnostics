using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Notification
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Enum representing Notification level.
        /// </summary>
        public InsightStatus Status;

        /// <summary>
        /// Notification title.
        /// </summary>
        public string Title;

        /// <summary>
        /// Notification description.
        /// </summary>
        public string Description;


        /// <summary>
        /// Optional solutions to common problems to which the notification may refer
        /// </summary>
        public Solution Solution;


        /// <summary>
        /// Whether a notification is expanded to begin with
        /// </summary>
        public bool IsExpanded;

        /// <summary>
        /// The startdate when the notification will be shown
        /// </summary>
        public DateTime StartDate;


        /// <summary>
        /// The end date to stop showing the notification
        /// </summary>
        public DateTime ExpiryDate;


        /// <summary>
        /// Creates an instance of notification class.
        /// </summary>
        /// <param name="status">Enum reprensenting notification level.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="description">Notification description.</param>
        /// <param name="solution">Solution to address the notification.</param>
        /// <param name="isExpanded">Whether a notification is expanded to begin with.</param>
        /// <param name="startDate">The startdate when the notification will be shown.</param>
        /// <param name="expiryDate">The end date to stop showing the notification.</param>
        public Notification(InsightStatus status, string title, string description = "", Solution solution = null, bool isExpanded = true, DateTime? startDate = null, DateTime? expiryDate = null)
        {
            Status = status;
            Title = title;
            Description = description;
            Solution = solution;
            IsExpanded = isExpanded;
            StartDate = startDate ?? DateTime.UtcNow;
            ExpiryDate = expiryDate ?? DateTime.MaxValue;
        }
    }

    public static class ResponseNotificationsExtension
    {
        public static DiagnosticData AddNotification(this Response response, Notification notification)
        {
            try
            {
                if (notification.Status == null || string.IsNullOrWhiteSpace(notification.Title))
                {
                    throw new Exception("Required attributes Status and Title cannot be null or empty for Notification.");
                }

                if (DateTime.Compare(notification.StartDate, notification.ExpiryDate) <= 0)
                {
                    throw new Exception("Invalid StartDate and ExpiryDate, ExpiryDate should be greater than StartDate.");
                }

                if (DateTime.Compare(notification.ExpiryDate, DateTime.UtcNow) <= 0)
                {
                    throw new Exception("Invalid ExpiryDate, ExpiryDate should be greater than current UTC datetime.");
                }

                var table = new DataTable();


                table.Columns.AddRange(new DataColumn[]
                {
                new DataColumn("Status", typeof(string)),
                new DataColumn("Title", typeof(string)),
                new DataColumn("Description", typeof(string)),
                new DataColumn("Solution", typeof(string)),
                new DataColumn("Expanded", typeof(string)),
                new DataColumn("StartDate", typeof(string)),
                new DataColumn("ExpiryDate", typeof(string))
                });

                table.Rows.Add(new string[]
                     {
                            notification.Status.ToString(),
                            notification.Title,
                            notification.Description,
                            JsonConvert.SerializeObject(notification.Solution),
                            notification.IsExpanded.ToString(),
                            JsonConvert.SerializeObject(notification.StartDate),
                            JsonConvert.SerializeObject(notification.ExpiryDate),
                     });

                var diagData = new DiagnosticData()
                {
                    Table = table,
                    RenderingProperties = new Rendering(RenderingType.Notification)
                };
                response.Dataset.Add(diagData);
                return diagData;
            }
            catch (Exception ex)
            {
                throw new Exception("Notification validation failed: " + ex.ToString());
            }
        }
    }
}
