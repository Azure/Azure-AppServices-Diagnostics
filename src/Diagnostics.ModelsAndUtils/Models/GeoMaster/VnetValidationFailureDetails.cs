using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class VnetValidationTestFailure
    {
        /// <summary>
        /// The name of the test that failed.
        /// </summary>
        public string TestName { get; set; }
        /// <summary>
        /// The details of what caused the failure, e.g. the blocking rule name, etc.
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// A class that describes the reason for a validation failure.
    /// </summary>
    public class VnetValidationFailureDetails
    {
        /// <summary>
        /// A flag describing whether or not validation failed.
        /// </summary>
        public bool Failed { get; set; }

        /// <summary>
        /// A list of tests that failed in the validation.
        /// </summary>
        public List<VnetValidationTestFailure> FailedTests { get; set; }
    }
}
