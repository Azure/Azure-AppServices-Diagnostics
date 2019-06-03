using System.Data;

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
        /// <returns>Diagnostic Data Object that represents email</returns>
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
        public static DiagnosticData AddEmail(this Response response, Email email)
        {
            if (email == null || string.IsNullOrWhiteSpace(email.Content)) return null;

            DataTable table = new DataTable("Data Summary");

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Content", typeof(string))
            });

            table.Rows.Add(new string[]
            {
                email.Content
            });

            var diagnosticData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Email)
            };

            response.Dataset.Add(diagnosticData);

            return diagnosticData;
        }

        /// <summary>
        /// Adds a email content to the response
        /// </summary>
        /// <param name="response">Response object</param>
        /// <param name="content">Email content</param>
        /// <returns>Diagnostic Data Object that represents email</returns>
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
        public static DiagnosticData AddEmail(this Response response, string content)
        {
            return AddEmail(response, new Email(content));
        }
    }
}
