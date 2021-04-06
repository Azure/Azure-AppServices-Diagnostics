using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.DataProviders
{
    internal class K8SELogAnalyticsLogDecorator : LogAnalyticsLogDecorator
    {
        public K8SELogAnalyticsLogDecorator(DataProviderContext context, ILogAnalyticsDataProvider dataProvider) : base(context, dataProvider)
        {
        }
    }
}
