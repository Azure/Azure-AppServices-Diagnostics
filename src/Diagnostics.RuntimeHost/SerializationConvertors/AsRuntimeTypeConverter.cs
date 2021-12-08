using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Diagnostics.RuntimeHost
{
    /// <summary>
    /// Converter copied from https://stackoverflow.com/questions/59308763/derived-types-properties-missing-in-json-response-from-asp-net-core-api
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
            if (value.GetType() != typeof(T))
            {
                JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
            }
            else
            {
                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IncludeFields = true,
                    WriteIndented = true
                };

                JsonSerializer.Serialize(writer, value, serializeOptions);
            }
        }
    }
}