using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using System.Collections.Generic;
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

            if (value?.InnerException != null)
            {
                writer.WriteStartObject("InnerException");
                JsonSerializer.Serialize<Exception>(writer, value?.InnerException, options);
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNull("InnerException");
            }

            writer.WriteEndObject();
        }
    }

    public class DevOpsGetBranchesConverter : JsonConverter<List<(string, bool)>>
    {
        public override List<(string, bool)> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<List<(string, bool)>>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, List<(string, bool)> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach ((string, bool) i in value)
            {
                writer.WriteStartObject();

                writer.WriteString("branchName", i.Item1);
                writer.WriteString("isMainBranch", i.Item2.ToString());

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    public class DevOpsMakePRConverter : JsonConverter<(GitPullRequest, GitRepository)>
    {
        public override (GitPullRequest, GitRepository) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<(GitPullRequest, GitRepository)>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, (GitPullRequest, GitRepository) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("prId", value.Item1.PullRequestId.ToString());
            writer.WriteString("webUrl", value.Item2.WebUrl);

            writer.WriteEndObject();
        }
    }
}