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
        public const string RequestIdHeaderName = "x-ms-request-id";

        /// <summary>
        /// Internal view header.
        /// </summary>
        public const string InternalViewHeader = "x-ms-internal-view";

        /// <summary>
        /// Internal client header.
        /// </summary>
        public const string InternalClientHeader = "x-ms-internal-client";

        /// <summary>
        /// Etag header name.
        /// </summary>
        public const string EtagHeaderName = "ETag";

        /// <summary>
        /// If-match header name.
        /// </summary>
        public const string IfMatchHeaderName = "If-Match";

        /// <summary>
        /// If-none header name.
        /// </summary>
        public const string IfNoneMatchHeaderName = "If-None-Match";

        /// <summary>
        /// User agent header name.
        /// </summary>
        public const string UserAgentHeaderName = "User-Agent";
    }
}
