using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Response DTO for POST /v1/wallet-links/initiate (201 Created).
/// Properties are nullable because the API contract may evolve —
/// [JsonExtensionData] captures any fields not explicitly modelled.
/// </summary>
public record WalletLinkInitiateResponse
{
    [JsonPropertyName("wallet_link_id")]
    public string? WalletLinkId { get; init; }

    [JsonPropertyName("link_url")]
    public string? LinkUrl { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; init; }
    
    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// Useful for detecting API contract drift in tests.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public class DuplicateWalletLinkInitiateResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("debug_id")]
    public string? DebugId { get; set; }

    [JsonPropertyName("details")]
    public List<ErrorDetail>? Details { get; set; }  // Was string, now a List
}

public class ErrorDetail
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("issue")]
    public string? Issue { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

