using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Response DTO for GET /health endpoint.
/// </summary>
public record HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }
    
    [JsonPropertyName("database")]
    public string? Database { get; init; }
    
    [JsonPropertyName("redis")]
    public string? Redis { get; init; }
    
    [JsonPropertyName("redisHost")]
    public string? RedisHost { get; init; }
    
    [JsonPropertyName("redisPort")]
    public int? RedisPort { get; init; }

    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}


