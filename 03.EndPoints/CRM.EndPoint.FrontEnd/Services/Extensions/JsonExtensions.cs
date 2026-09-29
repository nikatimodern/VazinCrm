using System;
using System.Text.Json;

namespace CRM.EndPoint.FrontEnd.Services.Extensions;

public static class JsonExtensions {
    public static T GetValue<T>(this JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return default;

        return GetValue<T>(property);
    }

    private static T GetValue<T>(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return default;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)element.GetString();

            if (typeof(T) == typeof(int))
                return (T)(object)element.GetInt32();

            if (typeof(T) == typeof(long))
                return (T)(object)element.GetInt64();

            if (typeof(T) == typeof(decimal))
                return (T)(object)element.GetDecimal();

            if (typeof(T) == typeof(double))
                return (T)(object)element.GetDouble();

            if (typeof(T) == typeof(float))
                return (T)(object)element.GetSingle();

            if (typeof(T) == typeof(bool))
                return (T)(object)element.GetBoolean();

            if (typeof(T) == typeof(DateTime))
                return (T)(object)element.GetDateTime();

            if (typeof(T) == typeof(DateTimeOffset))
                return (T)(object)element.GetDateTimeOffset();

            return element.Deserialize<T>();
        }
        catch
        {
            return default;
        }
    }
}