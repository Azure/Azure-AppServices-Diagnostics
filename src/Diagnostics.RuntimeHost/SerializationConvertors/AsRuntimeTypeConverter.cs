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
        private static Lazy<JsonSerializerOptions> _options = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            options.Converters.Add(new ExceptionConverter());

            return options;
        });

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
                JsonSerializer.Serialize(writer, value, _options.Value);
            }
        }
    }

    public class ExceptionConverter : JsonConverter<Exception>
    {
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<Exception>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("ClassName", value?.GetType()?.FullName);
            writer.WriteString("Message", value?.Message);
            writer.WriteString("Source", value?.Source);
            writer.WriteString("StackTraceString", value?.StackTrace);

            writer.WriteEndObject();
        }
    }
}