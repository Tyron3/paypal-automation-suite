using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Response DTO for GET /v1/customers/{customerId}/paypal-userinfo.
/// [JsonExtensionData] captures any fields not explicitly modelled
/// so tests can detect API contract drift.
/// </summary>
public record UserInfoResponse
{
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("paypal_account_id")]
    public string? PaypalAccountId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

