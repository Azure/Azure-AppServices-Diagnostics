// <copyright file="DetectorType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Defines whether the Detector is of type Analysis or not.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DetectorType
    {
        /// <summary>
        /// Detector means this is a normal detector
        /// </summary>
        Detector,

        /// <summary>
        /// Analysis means this is a detector that can act as an analysis
        /// </summary>
        Analysis
    }
}
