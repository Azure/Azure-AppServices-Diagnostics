using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace Diagnostics.ModelsAndUtils.Models.ChangeAnalysis
{
    /// <summary>
    /// This model represents the changes data captured for a changeset in ARM Resource
    /// </summary>
    public class ResourceChangesResponseModel
    {
        /// <summary>
        /// Timestamp of the change.
        /// </summary>
        public string TimeStamp;

        /// <summary>
        /// Category of the change belonging to <see cref="ChangeCategory"/>.
        /// </summary>
        public ChangeCategory Category;

        /// <summary>
        /// Level of the change belonging to <see cref="ChangeLevel"/>.
        /// </summary>
        public ChangeLevel Level;

        /// <summary>
        /// Display name (property name).
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Description of the change.
        /// </summary>
        public string Description;

        /// <summary>
        /// Old value.
        /// </summary>
        [JsonConverter(typeof(SingleOrArrayConverter<object>))]
        public string OldValue;

        /// <summary>
        /// New value.
        /// </summary>
        [JsonConverter(typeof(SingleOrArrayConverter<object>))]
        public string NewValue;

        /// <summary>
        /// Initiated By. It can be email address or Guid.
        /// </summary>
        public string InitiatedBy;

        /// <summary>
        /// Json path obtained from definition.
        /// </summary>
        public string JsonPath;
    }

    public enum ChangeCategory
    {
        Uncategorized = 0,
        Network,
        HostEnv,
        AppConfig,
        Envar,
        Others
    }

    public enum ChangeLevel
    {
        Noise = 0,
        Normal,
        Important
    }

    /// <summary>
    /// Custom JsonConverter used to convert single or array for the same json property.
    /// </summary>
    internal class SingleOrArrayConverter<T> : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
            {
                return token.ToObject<List<T>>();
            }
            return new List<T> { token.ToObject<T>() };
        }

        public override bool CanWrite
        {
            get { return false; }
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(List<T>));
        }
    }
}
