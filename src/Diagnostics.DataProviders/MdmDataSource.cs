// <copyright file="MdmDataSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// MDM data source.
    /// </summary>
    public enum MdmDataSource
    {
        /// <summary>
        /// Antares MDM data provider.
        /// </summary>
        Antares,

        /// <summary>
        /// Azure networking MDM data provider.
        /// </summary>
        Networking,

        /// <summary>
        /// Azure Container Apps MDM data provider.
        /// </summary>
        ContainerApps,
    }
}
