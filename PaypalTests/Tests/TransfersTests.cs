using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class TransfersTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up TransfersTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed TransfersTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task Transfers_ShouldReturn201()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;

            if (_requestContext == null)
                throw new InvalidOperationException("BASEURL request context not initialized");

            TestReporter.AddStep(testName, "Checking if Quote ID is available");

            // Check if we need to generate a quote first
            var quoteId = Environment.GetEnvironmentVariable("PAYPAL_QUOTE_ID");
            if (string.IsNullOrEmpty(quoteId) || quoteId == "N/A")
            {
                TestReporter.AddStep(testName, "Quote ID not available, generating a new quote first");
                TestReporter.AddStep(testName, "⚠️  NOTE: Quote generation requires an active wallet link. Run WalletLinkTests first or ensure PAYPAL_QUOTE_ID env var is set.");

                // Generate a quote
                var quoteRequest = QuotePayload.Create();
                var quoteHeaders = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Accept", "application/json" },
                    { "Authorization", $"Bearer {_accessToken}" },
                    { "X-Moo-Request-Id", $"stg-quote-{Guid.NewGuid()}" }
                };

                var quoteResponse = await _requestContext.PostAsync(
                    Endpoints.Quote,
                    new APIRequestContextOptions
                    {
                        Headers = quoteHeaders,
                        Data = JsonSerializer.Serialize(quoteRequest, JsonDefaults.Options)
                    });

                var quoteResponseText = await quoteResponse.TextAsync();
                
                // Add debugging info
                TestReporter.AddMetric(testName, "Quote Endpoint", Endpoints.Quote);
                TestReporter.AddMetric(testName, "Quote Response Status", quoteResponse.Status);
                TestReporter.AddMetric(testName, "Quote Response Body", quoteResponseText);
                
                if (quoteResponse.Status == 404)
                {
                    Assert.Fail($"Quote generation failed with 404. This usually means no active wallet link exists for this customer. " +
                        $"Please run WalletLinkTests first to create a wallet link, or manually set PAYPAL_QUOTE_ID environment variable. " +
                        $"Response: {quoteResponseText}");
                    return;
                }
                
                Assert.That(quoteResponse.Status, Is.EqualTo(201), 
                    $"Expected 201 from quote generation endpoint. Got {quoteResponse.Status}. Response: {quoteResponseText}");

                QuoteResponse? quote;
                try
                {
                    quote = JsonSerializer.Deserialize<QuoteResponse>(quoteResponseText, JsonDefaults.Options);
                }
                catch (JsonException ex)
                {
                    Assert.Fail($"Failed to parse quote response: {ex.Message}");
                    return;
                }

                Assert.That(quote?.QuoteId, Is.Not.Null.And.Not.Empty, "Expected quote to have a valid ID");
                quoteId = quote!.QuoteId;
                Environment.SetEnvironmentVariable("PAYPAL_QUOTE_ID", quoteId);
                TestReporter.AddStep(testName, $"Generated new quote with ID: {quoteId}");
            }
            else
            {
                TestReporter.AddStep(testName, $"Using existing Quote ID: {quoteId}");
            }

            TestReporter.AddStep(testName, "Sending authenticated POST request to generate a transfer");

            if (_requestContext == null)
                throw new InvalidOperationException("BASEURL request context not initialized");

            var request = TransfersPayload.Create();
            
            async Task<IAPIResponse> SendWithAuthorizationHeaderAsync(string authorizationHeader)
            {
                var headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Accept", "application/json" },
                    { "Authorization", authorizationHeader },
                    { "X-Moo-Request-Id", $"stg-initiate-{Guid.NewGuid()}" }
                };

                return await _requestContext.PostAsync(
                    Endpoints.Transfers,
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

            Assert.That(response.Status, Is.EqualTo(201), 
                $"Expected 201 from transfers generation endpoint. Got {response.Status}. Response: {responseText}");
            Assert.That(responseText, Is.Not.Null.And.Not.Empty, "Expected response body with transfer details");

            TransfersResponse? transfersResponse;
            try
            {
                transfersResponse = JsonSerializer.Deserialize<TransfersResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(transfersResponse, Is.Not.Null, "Deserialized response should not be null");

            // transferId
            Assert.That(transfersResponse!.TransferId, Is.Not.Null.And.Not.Empty,
                "Expected 'transferId' to be non-empty");

            // customerId
            Assert.That(transfersResponse.CustomerId, Is.Not.Null.And.Not.Empty,
                "Expected 'customerId' to be non-empty");

            // quoteId
            Assert.That(transfersResponse.QuoteId, Is.Not.Null.And.Not.Empty,
                "Expected 'quoteId' to be non-empty");

            // status
            Assert.That(transfersResponse.Status, Is.Not.Null.And.Not.Empty,
                "Expected 'status' to be non-empty");
            Assert.That(transfersResponse.Status, Is.EqualTo("COMPLETED"),
                "Expected transfer status to be 'COMPLETED'");

            // fees
            Assert.That(transfersResponse.Fees, Is.Not.Null.And.Not.Empty,
                "Expected at least one entry in 'fees'");
            for (var i = 0; i < transfersResponse.Fees!.Count; i++)
            {
                var fee = transfersResponse.Fees[i];
                Assert.That(fee.Name, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].name' to be non-empty");
                Assert.That(fee.Currency, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].currency' to be non-empty");
                Assert.That(fee.Amount, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].amount' to be non-empty");
            }

            // paypal details
            Assert.That(transfersResponse.PayPal, Is.Not.Null,
                "Expected 'paypal' object in response body");
            Assert.That(transfersResponse.PayPal!.TransactionId, Is.Not.Null.And.Not.Empty,
                "Expected 'paypal.transactionId' to be non-empty");
            Assert.That(transfersResponse.PayPal.ReceiverFee, Is.Not.Null,
                "Expected 'paypal.receiverFee' object");
            Assert.That(transfersResponse.PayPal.ReceiverFee!.Value, Is.Not.Null.And.Not.Empty,
                "Expected 'paypal.receiverFee.value' to be non-empty");
            Assert.That(transfersResponse.PayPal.ReceiverFee.Currency, Is.Not.Null.And.Not.Empty,
                "Expected 'paypal.receiverFee.currency' to be non-empty");

            // remittance details
            Assert.That(transfersResponse.Remittance, Is.Not.Null,
                "Expected 'remittance' object in response body");
            Assert.That(transfersResponse.Remittance!.RemittanceId, Is.Not.Null.And.Not.Empty,
                "Expected 'remittance.remittanceId' to be non-empty");

            // amounts
            Assert.That(transfersResponse.Amounts, Is.Not.Null,
                "Expected 'amounts' object in response body");
            Assert.That(transfersResponse.Amounts!.Source, Is.Not.Null,
                "Expected 'amounts.source' object");
            Assert.That(transfersResponse.Amounts.Source!.Value, Is.Not.Null.And.Not.Empty,
                "Expected 'amounts.source.value' to be non-empty");
            Assert.That(transfersResponse.Amounts.Source.Currency, Is.Not.Null.And.Not.Empty,
                "Expected 'amounts.source.currency' to be non-empty");
            Assert.That(transfersResponse.Amounts.DestinationCredited, Is.Not.Null,
                "Expected 'amounts.destinationCredited' object");
            Assert.That(transfersResponse.Amounts.DestinationCredited!.Value, Is.Not.Null.And.Not.Empty,
                "Expected 'amounts.destinationCredited.value' to be non-empty");
            Assert.That(transfersResponse.Amounts.DestinationCredited.Currency, Is.Not.Null.And.Not.Empty,
                "Expected 'amounts.destinationCredited.currency' to be non-empty");

            // timestamps
            Assert.That(transfersResponse.CreatedAt, Is.Not.Null.And.Not.Empty,
                "Expected 'createdAt' to be non-empty");
            Assert.That(DateTimeOffset.TryParse(transfersResponse.CreatedAt, out _), Is.True,
                "Expected 'createdAt' to be a valid date-time");
            Assert.That(transfersResponse.CompletedAt, Is.Not.Null.And.Not.Empty,
                "Expected 'completedAt' to be non-empty");
            Assert.That(DateTimeOffset.TryParse(transfersResponse.CompletedAt, out _), Is.True,
                "Expected 'completedAt' to be a valid date-time");

            // Add metrics
            TestReporter.AddMetric(testName, "Transfer ID", transfersResponse.TransferId ?? "N/A");
            TestReporter.AddMetric(testName, "Customer ID", transfersResponse.CustomerId ?? "N/A");
            TestReporter.AddMetric(testName, "Quote ID", transfersResponse.QuoteId ?? "N/A");
            TestReporter.AddMetric(testName, "Status", transfersResponse.Status ?? "N/A");
            TestReporter.AddMetric(testName, "PayPal Transaction ID", transfersResponse.PayPal?.TransactionId ?? "N/A");
            
            // Save transfer ID so WithdrawalTests can use it without manual configuration
            Environment.SetEnvironmentVariable("PAYPAL_TRANSACTION_ID", transfersResponse.PayPal?.TransactionId);
            
            TestReporter.AddMetric(testName, "Receiver Fee",
                $"{transfersResponse.PayPal?.ReceiverFee?.Value} {transfersResponse.PayPal?.ReceiverFee?.Currency}");
            TestReporter.AddMetric(testName, "Remittance ID", transfersResponse.Remittance?.RemittanceId ?? "N/A");
            TestReporter.AddMetric(testName, "Source Amount",
                $"{transfersResponse.Amounts?.Source?.Value} {transfersResponse.Amounts?.Source?.Currency}");
            TestReporter.AddMetric(testName, "Destination Amount Credited",
                $"{transfersResponse.Amounts?.DestinationCredited?.Value} {transfersResponse.Amounts?.DestinationCredited?.Currency}");
            TestReporter.AddMetric(testName, "Created At", transfersResponse.CreatedAt ?? "N/A");
            TestReporter.AddMetric(testName, "Completed At", transfersResponse.CompletedAt ?? "N/A");
            TestReporter.AddMetric(testName, "Fee Count", transfersResponse.Fees?.Count ?? 0);
            for (var i = 0; i < (transfersResponse.Fees?.Count ?? 0); i++)
            {
                var fee = transfersResponse.Fees![i];
                TestReporter.AddMetric(testName, $"Fee[{i}]", $"{fee.Name}: {fee.Amount} {fee.Currency}");
            }
            
            TestReporter.AddStep(testName, "Validated successful 201 response with TransfersResponse DTO");
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
