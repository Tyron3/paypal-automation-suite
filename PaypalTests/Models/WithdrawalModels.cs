using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Response DTO for GET /withdrawal endpoint.
/// </summary>
public record WithdrawalResponse
{
    [JsonPropertyName("amount_details")]
    public object? AmountDetails { get; init; }    
        
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; init; }
    
    [JsonPropertyName("sender_transaction_id")]
    public string? SenderTransactionId { get; init; }
    
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; init; }

    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}


