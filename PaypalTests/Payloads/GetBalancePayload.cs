namespace Paypal.Payloads;

/// <summary>
/// Payload factory for GetBalance_ShouldReturn200 test.
/// Centralises request parameters so the test method stays focused on behaviour.
/// </summary>
public static class GetBalancePayload
{
    /// <summary>
    /// Returns the customer ID used to query PayPal balance.
    /// Falls back to a default if the environment variable is not set.
    /// </summary>
    public static string GetCustomerId()
    {
        return Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_ID")
               ?? "test-customer-001";
    }
    
    /// <summary>
    /// Returns the customer identity ID used to query PayPal balance.
    /// Falls back to a default if the environment variable is not set.
    /// </summary>
    public static string GetCustomerIdentityGuid()
    {
        return Environment.GetEnvironmentVariable("PAYPAL_USERINFO_CUSTOMER_IDENTITYGUID")
               ?? "test-customer-001";
    }
}

