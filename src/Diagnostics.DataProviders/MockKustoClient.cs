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
        public async Task<DataTable> ExecuteQueryAsync(string query, string stampName, string requestId = null, string operationName = null)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException("stampName");
            }

            if (!string.IsNullOrWhiteSpace(operationName))
            {
                switch (operationName.ToLower())
                {
                    case KustoOperations.GetTenantIdForWindows:
                    case KustoOperations.GetTenantIdForLinux:
                        return await GetFakeTenantIdResults();
                    case KustoOperations.GetLatestDeployment:
                        return await GetLatestDeployment();
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

        private Task<DataTable> GetLatestDeployment()
        {
            DataTable table = new DataTable();

            table.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("LatestDeployment", typeof(DateTime))
            });

            table.Rows.Add(new object[] { DateTime.UtcNow });

            return Task.FromResult(table);
        }

        public Task<string> GetKustoQueryUriAsync(string stampName, string query)
        {
            if (string.IsNullOrWhiteSpace(stampName))
            {
                throw new ArgumentNullException("stampName");
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentNullException("query");
            }
            return Task.FromResult("https://fakekusto.windows.net/q=somequery");
        }
    }
}
