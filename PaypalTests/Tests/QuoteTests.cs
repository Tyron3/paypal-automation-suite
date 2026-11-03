using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class QuoteTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up QuoteTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed QuoteTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task Quote_ShouldReturn201()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;

            TestReporter.AddStep(testName, "Sending authenticated POST request to generate a quote");

            if (_requestContext == null)
                throw new InvalidOperationException("BASEURL request context not initialized");

            var request = QuotePayload.Create();

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
                    Endpoints.Quote,
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

            Assert.That(response.Status, Is.EqualTo(201), "Expected 201 from quote generation endpoint");
            Assert.That(responseText, Is.Not.Null.And.Not.Empty, "Expected response body with quote details");

            QuoteResponse? quoteResponse;
            try
            {
                quoteResponse = JsonSerializer.Deserialize<QuoteResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            // ...existing code...
            Assert.That(quoteResponse, Is.Not.Null, "Deserialized response should not be null");

            // quoteId
            Assert.That(quoteResponse!.QuoteId, Is.Not.Null.And.Not.Empty,
                "Expected 'quoteId' to be non-empty");
            Environment.SetEnvironmentVariable("PAYPAL_QUOTE_ID", quoteResponse.QuoteId);

            // calculationToken
            Assert.That(quoteResponse.CalculationToken, Is.Not.Null.And.Not.Empty,
                "Expected 'calculationToken' to be non-empty");

            // sourceAmount
            Assert.That(quoteResponse.SourceAmount, Is.Not.Null,
                "Expected 'sourceAmount' object in response body");
            Assert.That(quoteResponse.SourceAmount!.Currency, Is.Not.Null.And.Not.Empty,
                "Expected 'sourceAmount.currency' to be non-empty");
            Assert.That(quoteResponse.SourceAmount.Amount, Is.Not.Null.And.Not.Empty,
                "Expected 'sourceAmount.amount' to be non-empty");

            // destinationAmount
            Assert.That(quoteResponse.DestinationAmount, Is.Not.Null,
                "Expected 'destinationAmount' object in response body");
            Assert.That(quoteResponse.DestinationAmount!.Currency, Is.Not.Null.And.Not.Empty,
                "Expected 'destinationAmount.currency' to be non-empty");
            Assert.That(quoteResponse.DestinationAmount.Amount, Is.Not.Null.And.Not.Empty,
                "Expected 'destinationAmount.amount' to be non-empty");

            // fees
            Assert.That(quoteResponse.Fees, Is.Not.Null.And.Not.Empty,
                "Expected at least one entry in 'fees'");
            for (var i = 0; i < quoteResponse.Fees!.Count; i++)
            {
                var fee = quoteResponse.Fees[i];
                Assert.That(fee.Name, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].name' to be non-empty");
                Assert.That(fee.Currency, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].currency' to be non-empty");
                Assert.That(fee.Amount, Is.Not.Null.And.Not.Empty, $"Expected 'fees[{i}].amount' to be non-empty");
            }

            // exchangeRate
            Assert.That(quoteResponse.ExchangeRate, Is.Not.Null.And.Not.Empty,
                "Expected 'exchangeRate' to be present and non-empty");
            Assert.That(decimal.TryParse(quoteResponse.ExchangeRate, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var exchangeRateValue), Is.True,
                $"Expected 'exchangeRate' to be a valid decimal, got: '{quoteResponse.ExchangeRate}'");
            Assert.That(exchangeRateValue, Is.GreaterThan(0), "Expected 'exchangeRate' to be greater than zero");

            // quoteExpirationTime
            Assert.That(quoteResponse.QuoteExpirationTime, Is.Not.Null.And.Not.Empty,
                "Expected 'quoteExpirationTime' to be non-empty");
            Assert.That(DateTimeOffset.TryParse(quoteResponse.QuoteExpirationTime, out _), Is.True,
                "Expected 'quoteExpirationTime' to be a valid date-time");

            // Add every asserted response variable to the metrics
            TestReporter.AddMetric(testName, "Quote Response", quoteResponse);
            TestReporter.AddMetric(testName, "Quote ID", quoteResponse.QuoteId ?? "N/A");
            TestReporter.AddMetric(testName, "Calculation Token", quoteResponse.CalculationToken ?? "N/A");
            TestReporter.AddMetric(testName, "Source Amount",
                $"{quoteResponse.SourceAmount.Amount} {quoteResponse.SourceAmount.Currency}");
            TestReporter.AddMetric(testName, "Destination Amount",
                $"{quoteResponse.DestinationAmount.Amount} {quoteResponse.DestinationAmount.Currency}");
            TestReporter.AddMetric(testName, "Exchange Rate", quoteResponse.ExchangeRate ?? "N/A");
            TestReporter.AddMetric(testName, "Quote Expiration Time", quoteResponse.QuoteExpirationTime ?? "N/A");
            TestReporter.AddMetric(testName, "Fee Count", quoteResponse.Fees.Count);
            for (var i = 0; i < quoteResponse.Fees.Count; i++)
            {
                var fee = quoteResponse.Fees[i];
                TestReporter.AddMetric(testName, $"Fee[{i}]", $"{fee.Name}: {fee.Amount} {fee.Currency}");
            }

            // Surface any additional fields returned by the API (contract drift detection)
            if (quoteResponse.AdditionalProperties is { Count: > 0 })
            {
                foreach (var (key, value) in quoteResponse.AdditionalProperties)
                {
                    TestReporter.AddMetric(testName, $"Extra: {key}", value.ToString());
                }
            }

            TestReporter.AddStep(testName, "Validated successful 201 response with QuoteResponse DTO");
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
