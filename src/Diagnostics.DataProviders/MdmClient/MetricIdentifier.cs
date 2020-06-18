using System;

namespace Diagnostics.DataProviders
{
    /// <summary>
    /// Uniquely identifies a metric.
    /// </summary>
    /// <remarks>TODO: move to MDMetricsClient project.</remarks>
    public struct MetricIdentifier : IEquatable<MetricIdentifier>
    {
        /// <summary>
        /// The monitoring account.
        /// </summary>
        private string monitoringAccount;

        /// <summary>
        /// The metric namespace.
        /// </summary>
        private string metricNamespace;

        /// <summary>
        /// The metric name.
        /// </summary>
        private string metricName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricIdentifier"/> struct.
        /// </summary>
        /// <param name="monitoringAccount">The monitoring account.</param>
        /// <param name="metricNamespace">The metric namespace.</param>
        /// <param name="metricName">Name of the metric.</param>
        public MetricIdentifier(string monitoringAccount, string metricNamespace, string metricName)
        {
            if (string.IsNullOrEmpty(monitoringAccount) || monitoringAccount.Trim().Length == 0)
            {
                throw new ArgumentException("monitoringAccount cannot be null or empty.", nameof(monitoringAccount));
            }

            if (string.IsNullOrEmpty(metricNamespace) || metricNamespace.Trim().Length == 0)
            {
                throw new ArgumentException("metricNamespace cannot be null or empty.", nameof(metricNamespace));
            }

            if (string.IsNullOrEmpty(metricName) || metricName.Trim().Length == 0)
            {
                throw new ArgumentException("metricName cannot be null or empty.", nameof(metricName));
            }

            this.monitoringAccount = monitoringAccount;
            this.metricNamespace = metricNamespace;
            this.metricName = metricName;
        }

        /// <summary>
        /// Gets the monitoring account which this metric belongs to.
        /// </summary>
        public string MonitoringAccount
        {
            get
            {
                return this.monitoringAccount;
            }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("value is null or empty.");
                }

                this.monitoringAccount = value;
            }
        }

        /// <summary>
        /// Gets the namespace which this metric belongs to.
        /// </summary>
        public string MetricNamespace
        {
            get
            {
                return this.metricNamespace;
            }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("value is null or empty.");
                }

                this.metricNamespace = value;
            }
        }

        /// <summary>
        /// Gets the name of the metric.
        /// </summary>
        public string MetricName
        {
            get
            {
                return this.metricName;
            }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("value is null or empty.");
                }

                this.metricName = value;
            }
        }

        /// <summary>
        /// Checks the validity of this instance.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(this.monitoringAccount)
                   && !string.IsNullOrEmpty(this.metricNamespace)
                   && !string.IsNullOrEmpty(this.metricName);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(MetricIdentifier other)
        {
            return string.Equals(this.monitoringAccount, other.monitoringAccount, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(this.metricNamespace, other.metricNamespace, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(this.metricName, other.metricName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is MetricIdentifier && this.Equals((MetricIdentifier)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.monitoringAccount != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.monitoringAccount) : 0;
                hashCode = (hashCode * 397) ^ (this.metricNamespace != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.metricNamespace) : 0);
                hashCode = (hashCode * 397) ^ (this.metricName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.metricName) : 0);

                return hashCode;
            }
        }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <exception cref="System.ArgumentException">
        /// MonitoringAccount is null or empty.
        /// or
        /// MetricNamespace is null or empty.
        /// or
        /// MetricName is null or empty.
        /// </exception>
        public void Validate()
        {
            if (string.IsNullOrEmpty(this.MonitoringAccount))
            {
                throw new ArgumentException("MonitoringAccount is null or empty.");
            }

            if (string.IsNullOrEmpty(this.MetricNamespace))
            {
                throw new ArgumentException("MetricNamespace is null or empty.");
            }

            if (string.IsNullOrEmpty(this.MetricName))
            {
                throw new ArgumentException("MetricName is null or empty.");
            }
        }
    }
}
