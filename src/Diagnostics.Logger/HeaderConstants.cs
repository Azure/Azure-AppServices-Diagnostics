// <copyright file="HeaderConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

namespace Diagnostics.Logger
{
    /// <summary>
    /// Class for header constants.
    /// </summary>
    public static class HeaderConstants
    {
        /// <summary>
        /// Request id header name.
        /// </summary>
        public static readonly string RequestIdHeaderName = "x-ms-request-id";

        /// <summary>
        /// Internal view header.
        /// </summary>
        public static readonly string InternalViewHeader = "x-ms-internal-view";

        /// <summary>
        /// Internal client header.
        /// </summary>
        public static readonly string InternalClientHeader = "x-ms-internal-client";

        /// <summary>
        /// Client Request ID header.
        /// </summary>
        public static readonly string ClientRequestIdHeader = "x-ms-client-request-id";

        /// <summary>
        /// Client Object ID header.
        /// </summary>
        public static readonly string ClientObjectIdHeader = "x-ms-client-object-id";

        /// <summary>
        /// Client Principal Name header.
        /// </summary>
        public static readonly string ClientPrincipalNameHeader = "x-ms-client-principal-name";
   
        /// <summary>
        /// Client Principal Name header.
        /// </summary>
        public static readonly string GeomasterHostNameHeader = "x-ms-geomaster-hostname";

        /// <summary>
        /// Etag header name.
        /// </summary>
        public static readonly string EtagHeaderName = "ETag";

        /// <summary>
        /// If-match header name.
        /// </summary>
        public static readonly string IfMatchHeaderName = "If-Match";

        /// <summary>
        /// If-none header name.
        /// </summary>
        public static readonly string IfNoneMatchHeaderName = "If-None-Match";

        /// <summary>
        /// User agent header name.
        /// </summary>
        public static readonly string UserAgentHeaderName = "User-Agent";

        /// <summary>
        /// Json Content type header.
        /// </summary>
        public static readonly string JsonContentType = "application/json";
    }
}
