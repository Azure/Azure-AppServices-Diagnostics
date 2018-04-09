using Diagnostics.ModelsAndUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    class MockKustoClient: IKustoClient
    {
        public async Task<DataTable> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null)
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

            return new DataTable();
        }

        private Task<DataTable> GetFakeTenantIdResults()
        {
            DataTable table = new DataTable();

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Tenant", typeof(string)),
                new DataColumn("PublicHost", typeof(string))
            });

            table.Rows.Add(new string[2] { Guid.NewGuid().ToString(), "fakestamp.cloudapp.net" });

            return Task.FromResult(table);
        }

        private Task<DataTable> GetTestA()
        {
            DataTable table = new DataTable();

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("TestColumn", typeof(string))
            });

            return Task.FromResult(table);
        }
    }
}
