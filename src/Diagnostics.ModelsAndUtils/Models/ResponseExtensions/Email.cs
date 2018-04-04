using System;
using System.Collections.Generic;
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
