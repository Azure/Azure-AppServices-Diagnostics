using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    class MockKustoClient: IKustoClient
    {
        public async Task<DataTableResponseObject> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null)
        {
            if (!string.IsNullOrWhiteSpace(operationName))
            {
                switch (operationName.ToLower())
                {
                    case "gettenantidforstamp":
                        return await GetFakeTenantIdResults();

                    default:
                        return await GetTestA();
                }
            }

            switch(query)
            {
                case "TestA":
                    return await GetTestA();
            }

            return new DataTableResponseObject();
        }

        private Task<DataTableResponseObject> GetFakeTenantIdResults()
        {
            var tenantColumn = new DataTableResponseColumn();
            tenantColumn.ColumnName = "Tenant";
            tenantColumn.ColumnType = "string";
            tenantColumn.DataType = "String";

            var publicHostColumn = new DataTableResponseColumn();
            publicHostColumn.ColumnName = "PublicHost";
            publicHostColumn.ColumnType = "string";
            publicHostColumn.DataType = "String";

            var res = new DataTableResponseObject
            {
                Columns = new List<DataTableResponseColumn>(new[] { tenantColumn, publicHostColumn })
            };

            res.Rows = new string[1][];
            res.Rows[0] = new string[2] { Guid.NewGuid().ToString(), "fakestamp.cloudapp.net" };
            
            return Task.FromResult(res);
        }

        private Task<DataTableResponseObject> GetTestA()
        {
            var testColumn = new DataTableResponseColumn();
            testColumn.ColumnName = "TestColumn";
            testColumn.ColumnType = "System.string";
            testColumn.DataType = "string";
            
            var res = new DataTableResponseObject();
            res.Columns = new List<DataTableResponseColumn>(new[] { testColumn });
            
            return Task.FromResult(res);
        }
    }
}
