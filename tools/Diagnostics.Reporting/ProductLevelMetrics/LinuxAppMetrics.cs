using Diagnostics.DataProviders;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Diagnostics.Reporting
{
    public class LinuxAppMetrics : ProductLevelMetricsBase
    {
        protected override string GetProductIds()
        {
            return "'16170', '16333'";
        }

        public void Run(KustoClient ks, IConfiguration config)
        {
            string emailSubject = config["ProductLevel:Linux:Subject"].ToString();
            List<string> toList = config["ProductLevel:Linux:To"].ToString().Split(new char[] { ',', ';', ':' }).ToList();

            base.Run(ks, config, "Linux Apps", emailSubject, toList);
        }

        protected override bool ShowSubCategoryLevelData()
        {
            return true;
        }

        protected override string GetCategoriesToShowSubCategoryLevelData()
        {
            return "'Availability, Performance, and Application Issues', 'Open Source Technologies'";
        }
    }
}
