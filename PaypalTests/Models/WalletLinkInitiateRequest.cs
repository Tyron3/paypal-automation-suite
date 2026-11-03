using System.Text.Json.Serialization;

namespace Paypal.Models;

/// <summary>
/// Request DTO for POST /v1/wallet-links/initiate
/// </summary>
public record WalletLinkInitiateRequest
{
    [JsonPropertyName("customer_identity_guid")]
    public required string CustomerIdentityGuid { get; init; }
    
    [JsonPropertyName("customer_id")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("return_url")]
    public required string ReturnUrl { get; init; }

    [JsonPropertyName("country_code")]
    public required string CountryCode { get; init; }

    [JsonPropertyName("nationality")]
    public required string Nationality { get; init; }

    [JsonPropertyName("locale")]
    public required string Locale { get; init; }

    [JsonPropertyName("first_name")]
    public required string FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public required string LastName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("date_of_birth")]
    public required string DateOfBirth { get; init; }

    [JsonPropertyName("national_id")]
    public required string NationalId { get; init; }
}

