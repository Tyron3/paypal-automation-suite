using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class WalletLinkTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up WalletLinkTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed WalletLinkTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task LinkWallet_ShouldReturn201()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;

            TestReporter.AddStep(testName, "Sending authenticated POST request to initiate wallet link");

            if (_requestContext == null)
                throw new InvalidOperationException("BASEURL request context not initialized");

            var request = LinkWalletPayload.Create();
            
            // Override customer_id with a unique value per test run
            var uniqueCustomerId = $"test-cust-{Guid.NewGuid()}";
            request = request with { CustomerId = uniqueCustomerId };
            
            async Task<IAPIResponse> SendWithAuthorizationHeaderAsync(string authorizationHeader)
            {
                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Accept", "application/json" },
                    { "Authorization", $"Bearer {_accessToken}" },
                    { "X-Moo-Request-Id", $"stg-initiate-{Guid.NewGuid()}" }
                };

                return await _requestContext.PostAsync(
                    Endpoints.WalletLinkInitiate,
                    new APIRequestContextOptions
                    {
                        Headers = headers,
                        Data = JsonSerializer.Serialize(request, JsonDefaults.Options)
                    });
            }

            static string ToBearer(string tokenOrHeader)
            {
                // Remove accidental control characters that break HTTP header validation.
                var sanitized = tokenOrHeader.Replace("\r", "").Replace("\n", "").Trim();
                return sanitized.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? sanitized
                    : $"Bearer {sanitized}";
            }

            var stopwatch = Stopwatch.StartNew();
            var response = await SendWithAuthorizationHeaderAsync(ToBearer(_accessToken));
            stopwatch.Stop();

            var responseText = await response.TextAsync();
            var responseHeaders = string.Join("; ", response.Headers.Select(h => $"{h.Key}={h.Value}"));

            TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
            TestReporter.AddMetric(testName, "Response Status Code", response.Status);
            TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);
            TestReporter.AddMetric(testName, "Response Body", responseText);
            TestReporter.AddMetric(testName, "Response Headers", responseHeaders);

            Assert.That(response.Status, Is.EqualTo(201), "Expected 201 from wallet link initiation endpoint");
            Assert.That(responseText, Is.Not.Null.And.Not.Empty, "Expected response body with wallet-link session details");

            JsonElement responseJson;
            try
            {
                responseJson = JsonSerializer.Deserialize<JsonElement>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(responseJson.TryGetProperty("session_id", out var sessionIdElement), Is.True,
                "Expected 'session_id' in response body");
            Assert.That(sessionIdElement.GetString(), Is.Not.Null.And.Not.Empty,
                "Expected 'session_id' to be non-empty");

            Assert.That(responseJson.TryGetProperty("authorization_url", out var authorizationUrlElement), Is.True,
                "Expected 'authorization_url' in response body");
            Assert.That(authorizationUrlElement.GetString(), Is.Not.Null.And.Not.Empty,
                "Expected 'authorization_url' to be non-empty");

            Assert.That(responseJson.TryGetProperty("expires_at", out var expiresAtElement), Is.True,
                "Expected 'expires_at' in response body");
            var expiresAtRaw = expiresAtElement.GetString();
            Assert.That(expiresAtRaw, Is.Not.Null.And.Not.Empty,
                "Expected 'expires_at' to be non-empty");
            Assert.That(DateTimeOffset.TryParse(expiresAtRaw, out _), Is.True,
                "Expected 'expires_at' to be a valid date-time");

            TestReporter.AddMetric(testName, "Session ID", sessionIdElement.GetString() ?? "N/A");
            TestReporter.AddMetric(testName, "Authorization URL", authorizationUrlElement.GetString() ?? "N/A");
            TestReporter.AddMetric(testName, "Expires At", expiresAtRaw ?? "N/A");

            WalletLinkInitiateResponse? walletLinkResponse;
            try
            {
                walletLinkResponse = JsonSerializer.Deserialize<WalletLinkInitiateResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(walletLinkResponse, Is.Not.Null, "Deserialized response should not be null");
            TestReporter.AddMetric(testName, "Wallet Link ID", walletLinkResponse!.WalletLinkId ?? "N/A");
            TestReporter.AddMetric(testName, "Link URL", walletLinkResponse.LinkUrl ?? "N/A");
            TestReporter.AddMetric(testName, "Status", walletLinkResponse.Status ?? "N/A");
            TestReporter.AddMetric(testName, "Customer ID", walletLinkResponse.CustomerId ?? "N/A");
            TestReporter.AddStep(testName, "Validated successful 201 response with WalletLinkInitiateResponse DTO");
        }
        catch (Exception ex)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            TestReporter.AddStep(testName, $"Test failed with exception: {ex.Message}");
            TestReporter.AddMetric(testName, "Exception Type", ex.GetType().Name);
            throw;
        }
    }
    
    [Test]
    [Order(2)]
    public async Task DuplicateLinkWallet_ShouldReturn409()
    {
        var testName = TestContext.CurrentContext.Test.Name;

        if (_requestContext == null)
            throw new InvalidOperationException("Request context not initialized");

        TestReporter.AddStep(testName, "Sending authenticated POST request to initiate wallet link");

        var request = LinkWalletPayload.Create();
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "Accept", "application/json" },
            { "Authorization", $"Bearer {_accessToken}" },
            { "X-Moo-Request-Id", $"stg-initiate-{Guid.NewGuid()}" }
        };

        var stopwatch = Stopwatch.StartNew();
        var response = await _requestContext.PostAsync(
            Endpoints.WalletLinkInitiate,
            new APIRequestContextOptions
            {
                Headers = headers,
                Data = JsonSerializer.Serialize(request, JsonDefaults.Options)
            });
        stopwatch.Stop();

        var responseText = await response.TextAsync();

        TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
        TestReporter.AddMetric(testName, "Response Status Code", response.Status);
        TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);
        TestReporter.AddMetric(testName, "Response Body", responseText);
        TestReporter.AddMetric(testName, "Response Headers", string.Join("; ", response.Headers.Select(h => $"{h.Key}={h.Value}")));

        Assert.That(response.Status, Is.EqualTo(409), "Expected 409 from duplicate wallet link initiation");
        Assert.That(responseText, Is.Not.Null.And.Not.Empty, "Expected non-empty response body");

        DuplicateWalletLinkInitiateResponse? duplicateWalletLinkResponse;
        try
        {
            duplicateWalletLinkResponse = JsonSerializer.Deserialize<DuplicateWalletLinkInitiateResponse>(responseText, JsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Response body is not valid JSON: {ex.Message}");
            return;
        }

        Assert.That(duplicateWalletLinkResponse, Is.Not.Null, "Deserialized response should not be null");
        if (duplicateWalletLinkResponse is null) return;

        Assert.Multiple(() =>
        {
            Assert.That(duplicateWalletLinkResponse.Name, Is.EqualTo("ACTIVE_WALLET_LINK_EXISTS"));
            Assert.That(duplicateWalletLinkResponse.Message, Is.Not.Null);
            Assert.That(duplicateWalletLinkResponse.DebugId, Is.Not.Null);
            Assert.That(duplicateWalletLinkResponse.Details, Is.Not.Null);
        });

        TestReporter.AddMetric(testName, "Name", duplicateWalletLinkResponse.Name ?? "N/A");
        TestReporter.AddMetric(testName, "Debug Id", duplicateWalletLinkResponse.DebugId ?? "N/A");
        TestReporter.AddMetric(testName, "Details", duplicateWalletLinkResponse.Details);
        TestReporter.AddStep(testName, "Validated 409 conflict response with duplicate wallet link error details");
    }
}
