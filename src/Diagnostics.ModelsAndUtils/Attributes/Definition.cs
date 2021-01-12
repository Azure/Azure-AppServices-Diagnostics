﻿// <copyright file="Definition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Diagnostics.ModelsAndUtils.Models;
using Newtonsoft.Json;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Defines the properties of a detector definition
    /// </summary>
    public class Definition : Attribute, IEquatable<Definition>
    {
        public Definition()
        {
            this.instanceGUID = Guid.NewGuid();
        }

        /// <summary>
        /// Id of the detector(unique).
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// User-Friendly Name of the detector.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Description of the detector.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Author of the detector.
        /// </summary>
        [DataMember]
        public string Author { get; set; }

        /// <summary>
        /// Problem category. This serves as organizing group for detectors.
        /// </summary>
        /// <example>
        /// [Definition(Category = Categories.AvailabilityAndPerformance)]
        /// </example>
        [DataMember]
        public string Category { get; set; }

        /// <summary>
        /// List of Support Topics for which this detector is enabled.
        /// </summary>
        [DataMember]
        public IEnumerable<SupportTopic> SupportTopicList { get; set; }

        [JsonIgnore]
        public string AnalysisType { get; set; } = string.Empty;

        [JsonIgnore]
        private Guid instanceGUID;

        [JsonIgnore]
        public override object TypeId { get { return (object)instanceGUID; } }

        /// <summary>
        /// Gets Analysis Types for which this detector should apply to.
        /// </summary>
        [DataMember]
        public List<string> AnalysisTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AnalysisType))
                {
                    return null;
                }
                else
                {
                    return AnalysisType.Split(',').ToList();
                }
            }

            set
            {
                if (value == null)
                {
                    AnalysisType = string.Empty;
                }
                else
                {
                    AnalysisType = string.Join(",", value);
                }
            }
        }

        /// <summary>
        /// Whether this detector is an Analysis Detector or not.
        /// </summary>
        [DataMember]
        public DetectorType Type { get; set; }

        /// <summary>
        /// Gets or sets the score of a detector.
        /// </summary>
        public float Score { get; set; }

        public bool Equals(Definition other)
        {
            return Id == other.Id;
        }
    }
}
