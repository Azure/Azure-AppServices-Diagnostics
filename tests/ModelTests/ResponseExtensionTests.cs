using Diagnostics.ModelsAndUtils;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Xunit;

namespace Diagnostics.Tests.ModelTests
{
    public class ResponseExtensionTests
    {
        [Fact]
        public void TestAddInsightsExtension()
        {
            Insight insight = new Insight(InsightStatus.Critical, "some test message");
            Response res = new Response();

            var returnedInsight = res.AddInsight(insight);

            Assert.NotEmpty(res.Dataset);

            var insightFromDataSet = res.Dataset.FirstOrDefault();

            Assert.Equal(returnedInsight, insightFromDataSet);

            Assert.Equal<RenderingType>(RenderingType.Insights, insightFromDataSet.RenderingProperties.Type);

            foreach(DataRow row in insightFromDataSet.Table.Rows)
            {
                Assert.Equal(4, row.ItemArray.Count());
            }
        }
        
        [Fact]
        public void TestAddEmailExtension()
        {
            string emailContent = "<b>Test</b>";
            Response res = new Response();

            res.AddEmail(emailContent);

            Assert.NotEmpty(res.Dataset);
            Assert.Equal<RenderingType>(RenderingType.Email, res.Dataset.FirstOrDefault().RenderingProperties.Type);

            foreach (DataRow row in res.Dataset.FirstOrDefault().Table.Rows)
            {
                Assert.Single(row.ItemArray);
            }
        }

        [Fact]
        public void TestAddDataSummaryExtension()
        {
            DataSummary ds1 = new DataSummary("Title1", "40");
            DataSummary ds2 = new DataSummary("Title2", "60");
            DataSummary ds3 = new DataSummary("Title3", "80");

            Response res = new Response();

            res.AddDataSummary(new List<DataSummary>() { ds1, ds2, ds3 });

            Assert.NotEmpty(res.Dataset);
            Assert.Equal<RenderingType>(RenderingType.DataSummary, res.Dataset.FirstOrDefault().RenderingProperties.Type);

            foreach (DataRow row in res.Dataset.FirstOrDefault().Table.Rows)
            {
                Assert.Equal(3, row.ItemArray.Count());
            }
        }
    }
}
