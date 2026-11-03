using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Request DTO for POST /v1/transfers.
/// Used to initiate a transfer based on a quote.
/// </summary>
public record TransfersRequest
{
    [JsonPropertyName("customer_identity_guid")]
    public required string CustomerIdentityGuid { get; init; }

    [JsonPropertyName("customer_id")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("quote_id")]
    public required string QuoteId { get; init; }

    [JsonPropertyName("accept_quote")]
    public required bool AcceptQuote { get; init; }
}

/// <summary>
/// Represents a single amount with currency (transfers-specific).
/// </summary>
public record TransferMoney
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}

/// <summary>
/// PayPal-specific transaction details.
/// </summary>
public record PayPalDetails
{
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; init; }

    [JsonPropertyName("receiverFee")]
    public TransferMoney? ReceiverFee { get; init; }
}

/// <summary>
/// Remittance details for the transfer.
/// </summary>
public record RemittanceDetails
{
    [JsonPropertyName("remittanceId")]
    public string? RemittanceId { get; init; }
}

/// <summary>
/// Amount breakdown for source and destination.
/// </summary>
public record TransferAmounts
{
    [JsonPropertyName("source")]
    public TransferMoney? Source { get; init; }

    [JsonPropertyName("destinationCredited")]
    public TransferMoney? DestinationCredited { get; init; }
}

/// <summary>
/// Response DTO for POST /v1/transfers.
/// [JsonExtensionData] captures any fields not explicitly modelled
/// so tests can detect API contract drift.
/// </summary>
public record TransfersResponse
{
    [JsonPropertyName("transferId")]
    public string? TransferId { get; init; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; init; }

    [JsonPropertyName("quoteId")]
    public string? QuoteId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("fees")]
    public List<Fee>? Fees { get; init; }

    [JsonPropertyName("paypal")]
    public PayPalDetails? PayPal { get; init; }

    [JsonPropertyName("remittance")]
    public RemittanceDetails? Remittance { get; init; }

    [JsonPropertyName("amounts")]
    public TransferAmounts? Amounts { get; init; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; init; }

    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

