namespace Paypal;

/// <summary>
/// Centralised API endpoint paths.
/// Avoids magic strings scattered across tests and makes route changes a single-line fix.
/// </summary>
public static class Endpoints
{
    public const string WalletLinkInitiate = "/v1/wallet-links/initiate";

    public const string Quote = "/v1/quotes";
    
    public const string Transfers = "/v1/transfers";
    
    public static string UserInfo(string customerId, string customerIdentityGuid) =>
        $"/v1/customers/{customerId}/paypal-userinfo?customer_identity_guid={customerIdentityGuid}";
    
    public static string HealthCheck() =>
        $"/health";
    
    public static string Balance(string customerId, string customerIdentityGuid) =>
        $"/v1/customers/{customerId}/paypal-balance?customer_identity_guid={customerIdentityGuid}";
    
    public static string Withdrawal(string customerId, string customerIdentityGuid, string withdrawalId) =>
        $"/v1/customers/{customerId}/withdrawals/{withdrawalId}?customer_identity_guid={customerIdentityGuid}";
}

