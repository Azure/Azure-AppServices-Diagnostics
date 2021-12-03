using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diagnostics.RuntimeHost
{
    /// <summary>
    /// Converter copied from https://newbedev.com/derived-type-s-properties-missing-in-json-response-from-asp-net-core-api
    /// This is required to ensure all the derived classes get serialized properly by System.Text.Json
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsRuntimeTypeConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
        }
    }
}