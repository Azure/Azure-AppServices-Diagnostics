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

        public SupportTopicSTJCompat(SupportTopic st)
        {
            if (st == null)
            {
                throw new ArgumentNullException(nameof(st));
            }

            this.Id = st.Id;
            this.PesId = st.Id;
        }
    }
}
