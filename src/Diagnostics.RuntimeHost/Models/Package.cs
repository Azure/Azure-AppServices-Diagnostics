// <copyright file="Package.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Diagnostics.RuntimeHost.Utilities;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// Publishing package.
    /// </summary>
    public class Package
    {
        private string sanitizedCodeString;

        /// <summary>
        /// Gets or sets the code string.
        /// </summary>
        public string CodeString
        {
            get
            {
                return sanitizedCodeString;
            }

            set
            {
                sanitizedCodeString = FileHelper.SanitizeScriptFile(value);
            }
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the committed alias.
        /// </summary>
        public string CommittedByAlias { get; set; }

        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public string PackageConfig { get; set; }

        /// <summary>
        /// Gets or sets the dll bytes.
        /// </summary>
        public string DllBytes { get; set; }

        /// <summary>
        /// Gets or sets the pdb bytes.
        /// </summary>
        public string PdbBytes { get; set; }

        /// <summary>
        /// Get commit for detector package.
        /// </summary>
        /// <returns>The commit.</returns>
        public Commit GetCommit()
        {
            var content = new Dictionary<string, Tuple<string, bool>>();
            var filePath = $"{Id.ToLower()}/{Id.ToLower()}";
            var csxFilePath = $"{filePath}.csx";
            var dllFilePath = $"{filePath}.dll";
            var pdbFilePath = $"{filePath}.pdb";
            var configPath = $"{Id.ToLower()}/package.json";

            content.Add(csxFilePath, Tuple.Create(CodeString, true));
            content.Add(dllFilePath, Tuple.Create(DllBytes, false));
            content.Add(pdbFilePath, Tuple.Create(PdbBytes, false));
            content.Add(configPath, Tuple.Create(PackageConfig, true));

            return new Commit
            {
                Message = $"Package : {Id.ToLower()}, CommittedBy : {CommittedByAlias}",
                Content = content.ToImmutableDictionary()
            };
        }
    }
}
