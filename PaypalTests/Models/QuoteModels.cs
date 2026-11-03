using System.Text.Json;
using System.Text.Json.Serialization;
using Paypal.Infrastructure;

namespace Paypal.Models;

/// <summary>
/// Currency + amount pair used by the quote request and the
/// source/destination amounts on the response.
/// </summary>
public record TransferAmount
{
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("amount")]
    public string? Amount { get; init; }
}

/// <summary>
/// A single fee line item on a quote response.
/// </summary>
public record Fee
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("amount")]
    public string? Amount { get; init; }
}

/// <summary>
/// Request DTO for POST /v1/quotes.
/// </summary>
public record QuoteRequest
{
    [JsonPropertyName("customer_identity_guid")]
    public required string CustomerIdentityGuid { get; init; }

    [JsonPropertyName("customer_id")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("transfer_amount")]
    public required TransferAmount TransferAmount { get; init; }
}

/// <summary>
/// Response DTO for POST /v1/quotes.
/// [JsonExtensionData] captures any fields not explicitly modelled
/// so tests can detect API contract drift.
/// </summary>
public record QuoteResponse
{
    [JsonPropertyName("quoteId")]
    public string? QuoteId { get; init; }

    [JsonPropertyName("calculationToken")]
    public string? CalculationToken { get; init; }

    [JsonPropertyName("sourceAmount")]
    public TransferAmount? SourceAmount { get; init; }

    [JsonPropertyName("destinationAmount")]
    public TransferAmount? DestinationAmount { get; init; }

    [JsonPropertyName("fees")]
    public List<Fee>? Fees { get; init; }

    [JsonPropertyName("exchangeRate")]
    public string? ExchangeRate { get; init; }

    [JsonPropertyName("quoteExpirationTime")]
    public string? QuoteExpirationTime { get; init; }

    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

