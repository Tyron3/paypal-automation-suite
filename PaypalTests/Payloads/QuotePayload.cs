using Paypal.Models;

namespace Paypal.Payloads;

/// <summary>
/// Payload factory for Quote_ShouldReturn201 test.
/// Centralises the request data so the test method stays focused on behaviour.
/// </summary>
public static class QuotePayload
{
    /// <summary>
    /// Creates a valid quote generation request.
    /// </summary>
    public static QuoteRequest Create() => new()
    {
        CustomerIdentityGuid = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_IDENTITYGUID") ?? "N/A",
        CustomerId = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_ID") ?? "N/A",
        TransferAmount = new TransferAmount
        {
            Currency = "BWP",
            Amount = "20.00"
        }
    };
}

