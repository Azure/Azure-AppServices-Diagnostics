// <copyright file="ISourceWatcher.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>

using System.Threading.Tasks;
using Diagnostics.DataProviders.Interfaces;
using Diagnostics.RuntimeHost.Models;

namespace Diagnostics.RuntimeHost.Services.SourceWatcher
{
    /// <summary>
    /// Interface for source watcher.
    /// </summary>
    public interface ISourceWatcher : IHealthCheck
    {
        /// <summary>
        /// Start source watcher.
        /// </summary>
        void Start();

        /// <summary>
        /// Wait for iteration to complete.
        /// </summary>
        /// <returns>The task.</returns>
        Task WaitForFirstCompletion();

        /// <summary>
        /// Create or update package.
        /// </summary>
        /// <param name="pkg">The package.</param>
        /// <returns>Task for creating or updating package.</returns>
        Task CreateOrUpdatePackage(Package pkg);
    }
}
