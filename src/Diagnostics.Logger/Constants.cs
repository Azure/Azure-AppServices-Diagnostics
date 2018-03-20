using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.Logger
{
    public class HeaderConstants
    {
        public const string RequestIdHeaderName = "x-ms-request-id";
        public const string EtagHeaderName = "ETag";
        public const string IfMatchHeaderName = "If-Match";
        public const string IfNoneMatchHeaderName = "If-None-Match";
        public const string UserAgentHeaderName = "User-Agent";
    }
}
