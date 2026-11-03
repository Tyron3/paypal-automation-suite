using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Paypal.Infrastructure;

/// <summary>
/// Custom JSON converter that handles deserializing string values to decimal.
/// Useful for APIs that return numeric values as quoted strings.
/// </summary>
public class StringToDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;
                // Try parsing with invariant culture and common formats
                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    return result;
                // Fallback to current culture
                if (decimal.TryParse(stringValue, out var fallbackResult))
                    return fallbackResult;
                // If all parsing fails, return null (but you could also throw here)
                Console.WriteLine($"⚠️ Warning: Failed to parse exchangeRate string value: '{stringValue}'");
                return null;

            case JsonTokenType.Number:
                return reader.GetDecimal();

            case JsonTokenType.Null:
                return null;

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when parsing decimal value.");
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

