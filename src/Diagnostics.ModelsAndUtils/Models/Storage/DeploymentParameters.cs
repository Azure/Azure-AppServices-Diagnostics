﻿using System;
using System.Collections.Generic;

namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    /// <summary>
    /// Deployment Parameters used by client/service caller to deploy detectors.
    /// </summary>
    public class DeploymentParameters
    {
        /// <summary>
        /// Single commit id to deploy. Not applicable if <see cref="FromCommitId"/> and <see cref="ToCommitId"/> is given.
        /// </summary>
        public string CommitId;

        /// <summary>
        /// Start commit to deploy. Not applicable if <see cref="CommitId"/> is given.
        /// </summary>
        public string FromCommitId;

        /// <summary>
        /// End commit to deploy. Not applicable if <see cref="CommitId"/> is given.
        /// </summary>
        public string ToCommitId;

        /// <summary>
        /// If provided, includes detectors modified after this date. Cannot be combined with <see cref="FromCommitId"/> and <see cref="ToCommitId"/>.
        /// </summary>
        public string StartDate;

        /// <summary>
        /// If provided, includes detectors modified before this date. Cannot be combined with <see cref="FromCommitId"/> and <see cref="ToCommitId"/>.
        /// </summary>
        public string EndDate;

        /// <summary>
        /// Resource type of the caller. eg. Microsoft.Web/sites.
        /// </summary>
        public string ResourceType;
    }

    /// <summary>
    /// Deployment response sent back to the caller;
    /// </summary>
    public class DeploymentResponse
    {
        /// <summary>
        /// List of detectors that got updated/added/edited;
        /// </summary>
        public List<string> DeployedDetectors;

        /// <summary>
        /// List of detectors that failed deployment along with the reason of failure;
        /// </summary>
        public Dictionary<string, string> FailedDetectors;

        /// <summary>
        /// List of detectors that were marked for deletion;
        /// </summary>
        public List<string> DeletedDetectors;

        /// <summary>
        /// Unique Guid to track the deployment;
        /// </summary>
        public string DeploymentGuid;
    }
}
