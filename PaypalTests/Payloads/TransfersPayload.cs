using Paypal.Models;

namespace Paypal.Payloads;

/// <summary>
/// Payload factory for Quote_ShouldReturn201 test.
/// Centralises the request data so the test method stays focused on behaviour.
/// </summary>
public static class TransfersPayload
{
    /// <summary>
    /// Creates a valid quote generation request.
    /// </summary>
    public static TransfersRequest Create() => new()
    {
        CustomerIdentityGuid = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_IDENTITYGUID") ?? "N/A",
        CustomerId = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_ID") ?? "N/A",
        QuoteId = Environment.GetEnvironmentVariable("PAYPAL_QUOTE_ID") ?? "N/A",
        AcceptQuote = true
    };
}

