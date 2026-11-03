using static System.Environment;
using System.Text.Json;

namespace Paypal;

public static class PaymentTestHelpers
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Cache tokens to avoid duplicate requests
    private static readonly Dictionary<string, (string token, DateTime expiry)> TokenCache = new();

    public static async Task<string> GetAccessTokenAsync(string clientIdEnvVar, string clientSecretEnvVar, string tokenEnvVar, string? scopeEnvVar = null)
    {
        var cacheKey = $"{clientIdEnvVar}_{scopeEnvVar}";
        
        // Check cache first
        if (TokenCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
        {
            return cached.token;
        }
        
        var tokenEndpoint = GetEnvironmentVariable("KEYCLOAKURL");
        var clientId = GetEnvironmentVariable(clientIdEnvVar);
        var clientSecret = GetEnvironmentVariable(clientSecretEnvVar);
        var grantType = GetEnvironmentVariable("GRANTTYPE");

        var missing = new List<string>();
        if (string.IsNullOrEmpty(tokenEndpoint)) missing.Add("KEYCLOAKURL");
        if (string.IsNullOrEmpty(clientId)) missing.Add(clientIdEnvVar);
        if (string.IsNullOrEmpty(clientSecret)) missing.Add(clientSecretEnvVar);
        if (string.IsNullOrEmpty(grantType)) missing.Add("GRANTTYPE");
        if (missing.Count > 0)
            throw new InvalidOperationException($"Missing required environment variables: {string.Join(", ", missing)}. Ensure these are set in GitLab CI/CD variables or the .env file.");

        var requestParams = new Dictionary<string, string>
        {
            { "grant_type", grantType! },
            { "client_id", clientId! },
            { "client_secret", clientSecret! }
        };

        if (!string.IsNullOrEmpty(scopeEnvVar))
        {
            var scope = GetEnvironmentVariable(scopeEnvVar) ?? GetEnvironmentVariable("SCOPE");
            requestParams.Add("scope", scope ?? "");
        }

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestParams)
        };
        
        var tokenResponse = await Client.SendAsync(tokenRequest);

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"⚠️ Client ID or Secret might be wrong for {clientIdEnvVar}.");
            Assert.Fail($"Token request failed for {clientIdEnvVar}: {tokenResponse.StatusCode}");
        }

        JsonElement tokenData;
        try
        {
            tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent, JsonDefaults.Options);
        }
        catch (JsonException e)
        {
            Assert.Fail($"Failed to parse token response for {clientIdEnvVar}: {e.Message}");
            return "";
        }

        if (!tokenData.TryGetProperty("access_token", out var tokenElement))
        {
            Assert.Fail($"access_token not found in response for {clientIdEnvVar}.");
            return "";
        }

        var accessToken = tokenElement.GetString();
        SetEnvironmentVariable(tokenEnvVar, accessToken);
        
        // Cache the token (assume 1 hour expiry)
        TokenCache[cacheKey] = (accessToken ?? "", DateTime.UtcNow.AddHours(1));
        return accessToken ?? "";
    }
}
