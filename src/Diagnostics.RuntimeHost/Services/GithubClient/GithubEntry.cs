// <copyright file="GithubEntry.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Diagnostics.RuntimeHost.Services
{
    /// <summary>
    /// Github entry.
    /// </summary>
    public class GithubEntry
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonProperty("path")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the SHA.
        /// </summary>
        public string Sha { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        public string Download_url { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }
    }
}
