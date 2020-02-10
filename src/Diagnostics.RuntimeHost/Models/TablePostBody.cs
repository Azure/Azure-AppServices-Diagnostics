// <copyright file="TablePostBody.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// C# POCO object to represent tabular data.
    /// </summary>
    public sealed class TablePostBody
    {
        public IEnumerable<string> Columns { get; set; }
        public IEnumerable<string[]> Rows { get; set; }
    }
}
