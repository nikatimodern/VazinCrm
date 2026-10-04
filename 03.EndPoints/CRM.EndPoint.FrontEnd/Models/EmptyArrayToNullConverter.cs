using System;

namespace CRM.EndPoint.FrontEnd.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

public class EmptyArrayToNullConverter<T> : JsonConverter<T> where T : class {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            using var document = JsonDocument.ParseValue(ref reader);

//            if (document.RootElement.GetArrayLength() == 0)
            return null;

//            throw new JsonException("Expected object or empty array, but got non-empty array.");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}