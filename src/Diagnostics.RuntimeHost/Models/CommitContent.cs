// <copyright file="Commit.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Octokit;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Class for github commit.
    /// </summary>
    public class CommitContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitContent"/> class.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="content">Commit content.</param>
        /// <param name="encodingType">Encoding type.</param>
        public CommitContent(string filePath, string content, EncodingType encodingType = EncodingType.Utf8)
        {
            FilePath = filePath;
            Content = content;
            EncodingType = encodingType;
        }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the encoding type.
        /// </summary>
        public EncodingType EncodingType { get; set; }
    }
}
