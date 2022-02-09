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
        /// Sap Support Topic Id
        /// </summary>
        public string SapSupportTopicId;

        /// <summary>
        /// Sap product Id
        /// </summary>
        public string SapProductId;

        /// <summary>
        /// Unique resource Id.
        /// </summary>
        public string PesId;

        public bool Equals(SupportTopic other)
        {
            return (this.Id == other.Id && this.PesId == other.PesId || this.SapSupportTopicId == other.SapSupportTopicId && this.SapProductId == other.SapProductId);
        }
    }

    /// <summary>
    /// Class created just for Json Serialization as Attributes 
    /// are not serialized properly to Json using System.Text.Json
    /// https://github.com/dotnet/runtime/issues/58947
    /// </summary>
    public class SupportTopicSTJCompat
    {
        /// <summary>
        /// Support Topic Id
        /// </summary>
        /// See <see href="http://aka.ms/selfhelppreview"/>
        public string Id { get; set; }

        /// <summary>
        /// Unique resource Id.
        /// </summary>
        public string PesId { get; set; }

        /// <summary>
        /// Sap Support Topic Id
        /// </summary>
        public string SapSupportTopicId { get; set; }

        /// <summary>
        /// Sap product Id
        /// </summary>
        public string SapProductId { get; set; }

        public SupportTopicSTJCompat(SupportTopic st)
        {
            if (st == null)
            {
                throw new ArgumentNullException(nameof(st));
            }

            this.Id = st.Id;
            this.PesId = st.PesId;
            this.SapSupportTopicId = st.SapSupportTopicId;
            this.SapProductId = st.SapProductId;
        }
    }
}
