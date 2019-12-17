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
        /// Geomaster HostName header.
        /// </summary>
        public static readonly string GeomasterHostNameHeader = "x-ms-geomaster-hostname";

        /// <summary>
        /// Geomaster Name header.
        /// </summary>
        public static readonly string GeomasterNameHeader = "x-ms-geomaster-name";

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

        /// <summary>
        /// Client Issuer header.
        /// </summary>
        public static readonly string ClientIssuerHeader = "x-ms-client-issuer";

        /// <summary>
        /// Client Puid header.
        /// </summary>
        public static readonly string ClientPuidHeader = "x-ms-client-puid";

        /// <summary>
        /// Client Alt SecId header.
        /// </summary>
        public static readonly string ClientAltSecIdHeader = "x-ms-client-alt-sec-id";

        /// <summary>
        /// Client Identity Provider header.
        /// </summary>
        public static readonly string ClientIdentityProviderHeader = "x-ms-client-identity-provider";

        /// <summary>
        /// Subscription Location Placement id.
        /// </summary>
        public static readonly string SubscriptionLocationPlacementId = "x-ms-subscription-location-placementid";
    }
}
