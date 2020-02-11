// <copyright file="Table.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Diagnostics.RuntimeHost.Models
{
    /// <summary>
    /// C# POCO object to represent tabular data.
    /// </summary>
    public sealed class Table : IEquatable<Table>
    {
        public IEnumerable<string> Columns { get; set; }
        public IEnumerable<string[]> Rows { get; set; }

        public bool Equals(Table other)
        {
            if (other == null)
                return false;

            var areColumnEqual = this.Columns.All(x => other.Columns.Contains(x, StringComparer.CurrentCultureIgnoreCase));
            var areRowsEqual = this.Rows.All(row => row.All(val => other.Rows.Any(otherRow => otherRow.Contains(val, StringComparer.CurrentCultureIgnoreCase))));
            return areColumnEqual && areRowsEqual;
        }
    }
}
