using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    /// <summary>
    /// Result for search detectors query.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Gets or sets Detector Id.
        /// </summary>
        public string Detector { get; set; }

        /// <summary>
        /// Gets or sets Score.
        /// </summary>
        public float Score { get; set; }
    }

    /// <summary>
    /// Results for search detectors query.
    /// </summary>
    public class SearchResults
    {
        /// <summary>
        /// Gets or sets Search Query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets Array of Search Results.
        /// </summary>
        public SearchResult[] Results { get; set; }
    }

    /// <summary>
    /// Results for utterances query.
    /// </summary>
    public class QueryUtterancesResults
    {
        /// <summary>
        /// Gets or sets Search Query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets Array of utterance results for search query.
        /// </summary>
        public QueryUtterancesResult[] Results { get; set; }
    }

    /// <summary>
    /// Result for utterances query.
    /// </summary>
    public class QueryUtterancesResult
    {
        /// <summary>
        /// Gets or sets a sample utterance.
        /// </summary>
        public SampleUtterance SampleUtterance { get; set; }

        /// <summary>
        /// Gets or sets the score of a sample utterance.
        /// </summary>
        public float Score { get; set; }
    }

    /// <summary>
    /// Sample utterance.
    /// </summary>
    public class SampleUtterance
    {
        /// <summary>
        /// Gets or sets Text attribute of sample utterance.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets Links attribute of sample utterance.
        /// </summary>
        public string[] Links { get; set; }

        /// <summary>
        /// Gets or sets Question id of sample utterance (for stackoverflow questions titles).
        /// </summary>
        public string Qid { get; set; }
    }
}
