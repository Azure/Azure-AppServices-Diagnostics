using System;

namespace Diagnostics.DataProviders
{
    public class DataSourceCancelledException : Exception
    {
        public DataSourceCancelledException() : base()
        {
        }

        public DataSourceCancelledException(string message) : base(message)
        {
        }

        public DataSourceCancelledException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
