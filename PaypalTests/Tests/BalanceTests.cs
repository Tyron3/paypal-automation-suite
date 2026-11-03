using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class BalanceTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up BalanceTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed BalanceTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task GetBalance_ShouldReturn200()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var customerId = GetBalancePayload.GetCustomerId();
            var customerIdentityGuid = GetBalancePayload.GetCustomerIdentityGuid();

            TestReporter.AddStep(testName, "Sending authenticated GET request for get balance endpoint");

            if (_requestContext == null)
                throw new InvalidOperationException("BASEURL request context not initialized");

            var extraHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_accessToken}" },
                { "X-Moo-Request-Id", $"REQ{timestamp}" }
            };

            var options = new APIRequestContextOptions
            {
                Headers = extraHeaders
            };

            var stopwatch = Stopwatch.StartNew();
            var response = await _requestContext.GetAsync(
                Endpoints.Balance(customerId, customerIdentityGuid),
                options);
            stopwatch.Stop();

            var responseText = await response.TextAsync();

            TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
            TestReporter.AddMetric(testName, "Response Status Code", response.Status);
            TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);

            Assert.That(response.Status, Is.EqualTo(200), "Expected successful response from customer PayPal user info endpoint");
            Assert.That(responseText, Is.Not.Empty, "Response body should not be empty");

            BalanceResponse? balance;
            try
            {
                balance = JsonSerializer.Deserialize<BalanceResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(balance, Is.Not.Null, "Deserialized response should not be null");
            
            Assert.Multiple(() =>
            {
                // total_available
                Assert.That(balance.TotalAvailable, Is.Not.Null, "TotalAvailable should not be null");
                Assert.That(balance.TotalAvailable!.CurrencyCode, Is.EqualTo("USD"));

                // total_reserved
                Assert.That(balance.TotalReserved, Is.Not.Null, "TotalReserved should not be null");
                Assert.That(balance.TotalReserved!.CurrencyCode, Is.EqualTo("USD"));
                Assert.That(balance.TotalReserved!.Value, Is.EqualTo("0.00"));

                // balance_accounts
                Assert.That(balance.BalanceAccounts, Is.Not.Null, "BalanceAccounts should not be null");
                Assert.That(balance.BalanceAccounts, Has.Count.EqualTo(1));
                Assert.That(balance.BalanceAccounts![0].Available!.CurrencyCode, Is.EqualTo("USD"));
                Assert.That(balance.BalanceAccounts![0].Reserved!.CurrencyCode, Is.EqualTo("USD"));
                Assert.That(balance.BalanceAccounts![0].Reserved!.Value, Is.EqualTo("0.00"));
            });

            TestReporter.AddMetric(testName, "Total Available", balance.TotalAvailable!);
            TestReporter.AddMetric(testName, "Total Reserved", balance.TotalReserved!);
            TestReporter.AddMetric(testName, "Balance accounts", balance.BalanceAccounts);
            TestReporter.AddStep(testName, "Validated successful 200 response with BalanceResponse DTO");
        }
        catch (Exception ex)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            TestReporter.AddStep(testName, $"Test failed with exception: {ex.Message}");
            TestReporter.AddMetric(testName, "Exception Type", ex.GetType().Name);
            throw;
        }
    }
}

