using System;

namespace Diagnostics.ModelsAndUtils.Attributes
{
    /// <summary>
    /// Defines a unique Support Topic
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SupportTopic : Attribute, IEquatable<SupportTopic>
    {
        /// <summary>
        /// Support Topic Id
        /// </summary>
        /// See <see href="http://aka.ms/selfhelppreview"/>
        public string Id;

        /// <summary>
        /// Unique resource Id.
        /// </summary>
        public string PesId;

        public bool Equals(SupportTopic other)
        {
            return (this.Id == other.Id && this.PesId == other.PesId);
        }
    }
}
