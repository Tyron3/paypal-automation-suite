using Paypal.Models;

namespace Paypal.Payloads;

/// <summary>
/// Payload factory for LinkWallet_ShouldReturn201 test.
/// Centralises the request data so the test method stays focused on behaviour.
/// </summary>
public static class LinkWalletPayload
{
    private const string DefaultReturnUrl =
        "https://paypal-transfer-handler-staging.app-stg.mukuru.io/wallet-link-result";

    /// <summary>
    /// Creates a valid wallet-link initiation request with a unique customer ID.
    /// </summary>
    public static WalletLinkInitiateRequest Create()
    {
        var randomInt = Random.Shared.Next(100000, 999999);

        return new WalletLinkInitiateRequest
        {
            CustomerIdentityGuid = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_IDENTITYGUID") ?? "N/A",
            CustomerId = Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_ID") ?? "N/A",
            ReturnUrl = Environment.GetEnvironmentVariable("RETURNURL") ?? DefaultReturnUrl,
            CountryCode = "ZA",
            Nationality = "ZA",
            Locale = "en-ZA",
            FirstName = "John",
            LastName = "Doe",
            Email = "sb-m43bag49958542@personal.example.com",
            Phone = "+27878288415",
            DateOfBirth = "1990-01-09",
            NationalId = "900115012390"
        };
    }
}

