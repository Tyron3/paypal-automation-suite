using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Response DTO for GET /balance endpoint.
/// </summary>
public class BalanceResponse
{
    [JsonPropertyName("total_available")]
    public MoneyAmount? TotalAvailable { get; set; }

    [JsonPropertyName("total_reserved")]
    public MoneyAmount? TotalReserved { get; set; }

    [JsonPropertyName("balance_accounts")]
    public List<BalanceAccount>? BalanceAccounts { get; set; }
    
    /// <summary>
    /// Catches any additional JSON fields not mapped above.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public class MoneyAmount
{
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class BalanceAccount
{
    [JsonPropertyName("available")]
    public MoneyAmount? Available { get; set; }

    [JsonPropertyName("reserved")]
    public MoneyAmount? Reserved { get; set; }
}


