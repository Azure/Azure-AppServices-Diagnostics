using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    /// <summary>
    /// Class representing Email Content
    /// </summary>
    public class Email
    {
        /// <summary>
        /// Content
        /// </summary>
        public string Content;

        /// <summary>
        /// Creates an instance of Email class.
        /// </summary>
        /// <param name="content">Content</param>
        public Email(string content)
        {
            Content = content ?? string.Empty;
        }
    }

    public static class ResponseEmailExtension
    {
        /// <summary>
        /// Adds a email content to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="email">Email object</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddEmail"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     Email email = new Email("email content");
        ///     res.AddEmail(email);
        /// }
        /// </code>
        /// </example>
        public static void AddEmail(this Response response, Email email)
        {
            if (email == null || string.IsNullOrWhiteSpace(email.Content)) return;

            List<DataTableResponseColumn> columns = PrepareColumnDefinitions();
            List<string[]> rows = new List<string[]>
            {
                new List<string>() { email.Content }.ToArray()
            };

            DataTableResponseObject table = new DataTableResponseObject()
            {
                Columns = columns,
                Rows = rows.ToArray()
            };

            response.Dataset.Add(new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Email)
            });
        }

        /// <summary>
        /// Adds a email content to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="content">Email content</param>
        /// <example> 
        /// This sample shows how to use <see cref="AddEmail"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     string emailContentInHtml = "<![CDATA[<b>Test</b>]]>";
        ///     res.AddEmail(emailContentInHtml);
        /// }
        /// </code>
        /// </example>
        public static void AddEmail(this Response response, string content)
        {
            AddEmail(response, new Email(content));
        }

        private static List<DataTableResponseColumn> PrepareColumnDefinitions()
        {
            List<DataTableResponseColumn> columnDefinitions = new List<DataTableResponseColumn>
            {
                new DataTableResponseColumn() { ColumnName = "Content" }
            };

            return columnDefinitions;
        }
    }
}
