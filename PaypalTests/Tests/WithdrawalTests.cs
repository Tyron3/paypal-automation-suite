using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class WithdrawalTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up WithdrawalTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed WithdrawalTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task GetWithdrawal_ShouldReturn200()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var customerId = WithdrawalPayload.GetCustomerId();
            var customerIdentityGuid = WithdrawalPayload.GetCustomerIdentityGuid();
            var withdrawalId = WithdrawalPayload.GetWithdrawalId();

            Assert.That(withdrawalId, Is.Not.Null, "PAYPAL_WITHDRAWAL_ID or PAYPAL_TRANSACTION_ID environment variable must be set");
            TestReporter.AddStep(testName, "Sending authenticated GET request for customer PayPal withdrawal details");

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
                Endpoints.Withdrawal(customerId, customerIdentityGuid, withdrawalId!),
                options);
            stopwatch.Stop();

            var responseText = await response.TextAsync();

            TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
            TestReporter.AddMetric(testName, "Response Status Code", response.Status);
            TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);
            TestReporter.AddMetric(testName, "Response Body", responseText);
            TestReporter.AddMetric(testName, "Withdrawal ID Used", withdrawalId);
            TestReporter.AddMetric(testName, "Customer ID Used", customerId);

            Assert.That(response.Status, Is.EqualTo(200), 
                $"Expected 200 from withdrawal endpoint. Got {response.Status}. Withdrawal ID: '{withdrawalId}'. Response: {responseText}");
            Assert.That(responseText, Is.Not.Empty, "Response body should not be empty");

            WithdrawalResponse? withdrawal;
            try
            {
                withdrawal = JsonSerializer.Deserialize<WithdrawalResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(withdrawal, Is.Not.Null, "Deserialized response should not be null");
            Assert.That(withdrawal.AmountDetails, Is.Not.Null, "Withdrawal amount details should not be null");
            Assert.That(withdrawal.Status, Is.Not.Null.And.Not.Empty, "Withdrawal status should not be null or empty");
            Assert.That(withdrawal.Status, Is.EqualTo("COMPLETED"), "Withdrawal status should be set to completed");
            Assert.That(withdrawal.TransactionId, Is.Not.Null.And.Not.Empty, "Withdrawal transaction id should not be null or empty");
            Assert.That(withdrawal.SenderTransactionId, Is.Not.Null.And.Not.Empty, "Withdrawal sender transaction id should not be null or empty");
            Assert.That(withdrawal.CreateTime, Is.Not.Null.And.Not.Empty, "Withdrawal create time should not be null or empty");

            TestReporter.AddMetric(testName, "Status", withdrawal.Status ?? "N/A");
            TestReporter.AddMetric(testName, "Transaction ID", withdrawal.TransactionId ?? "N/A");
            TestReporter.AddMetric(testName, "Sender Transaction ID", withdrawal.SenderTransactionId ?? "N/A");
            TestReporter.AddMetric(testName, "Create Time", withdrawal.CreateTime ?? "N/A");
            TestReporter.AddStep(testName, "Validated successful 200 response with WithdrawalResponse DTO");
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

