using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;

namespace Paypal;

[TestFixture]
public class HealthCheckTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up HealthCheckTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed HealthCheckTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task GetHealthCheck_ShouldReturn200()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            TestReporter.AddStep(testName, "Sending authenticated GET request for health check");

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
                Endpoints.HealthCheck(),
                options);
            stopwatch.Stop();

            var responseText = await response.TextAsync();

            TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
            TestReporter.AddMetric(testName, "Response Status Code", response.Status);
            TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);

            Assert.That(response.Status, Is.EqualTo(200), "Expected successful response from customer PayPal user info endpoint");
            Assert.That(responseText, Is.Not.Empty, "Response body should not be empty");

            HealthCheckResponse? healthInfo;
            try
            {
                healthInfo = JsonSerializer.Deserialize<HealthCheckResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(healthInfo, Is.Not.Null, "Deserialized response should not be null");
            Assert.That(healthInfo.Status, Is.Not.Null.And.Not.Empty, "Status should not be null or empty");
            Assert.That(healthInfo.Database, Is.Not.Null.And.Not.Empty, "Database should not be null or empty");
            Assert.That(healthInfo.Redis, Is.Not.Null.And.Not.Empty, "Redis should not be null or empty");
            Assert.That(healthInfo.Timestamp, Is.Not.Null.And.Not.Empty, "Timestamp should not be null or empty");
            Assert.That(healthInfo.RedisHost, Is.Not.Null.And.Not.Empty, "RedisHost not be null or empty");
            Assert.That(healthInfo.RedisPort, Is.GreaterThan(0), "RedisPort should be a positive integer");
            
            TestReporter.AddMetric(testName, "Status", healthInfo.Status ?? "N/A");
            TestReporter.AddMetric(testName, "Database", healthInfo.Database ?? "N/A");
            TestReporter.AddMetric(testName, "Redis", healthInfo.Redis ?? "N/A");
            TestReporter.AddMetric(testName, "Timestamp", healthInfo.Timestamp ?? "N/A");
            TestReporter.AddMetric(testName, "RedisHost", healthInfo.RedisHost ?? "N/A");
            TestReporter.AddMetric(testName, "RedisPort", healthInfo.RedisPort);
            TestReporter.AddStep(testName, "Validated successful 200 response with HealthCheckResponse DTO");
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

