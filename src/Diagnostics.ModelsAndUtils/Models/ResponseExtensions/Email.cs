using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils
{
    public class Email
    {
        public string Content;

        public Email(string content)
        {
            Content = content ?? string.Empty;
        }
    }

    public static class ResponseEmailExtension
    {
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

        public static DiagnosticData AddEmail(this Response response, string content)
        {
            return AddEmail(response, new Email(content));
        }
    }
}
