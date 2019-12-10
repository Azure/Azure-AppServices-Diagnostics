using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseSearchComponentExtension
    {
        public static DiagnosticData AddSearch(this Response response)
        {
            var table = new DataTable();
            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.SearchComponent)
            };
            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}
