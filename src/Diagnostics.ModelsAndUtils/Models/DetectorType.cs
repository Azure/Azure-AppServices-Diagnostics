// <copyright file="DetectorType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>


using System.Text.Json.Serialization;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Defines whether the Detector is of type Analysis or not.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DetectorType
    {
        /// <summary>
        /// Detector means this is a normal detector
        /// </summary>
        Detector,

        /// <summary>
        /// Analysis means this is a detector that can act as an analysis
        /// </summary>
        Analysis,

        /// <summary>
        /// CategoryOverview means is the overpage for each category in external UI
        /// </summary>
        CategoryOverview,

        /// <summary>
        /// DiagnosticTool means this is a non-detector diagnostic tool implemented in diag portal
        /// </summary>
        DiagnosticTool,
    }
}
