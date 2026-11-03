using Microsoft.Playwright;
using System.Text.Json;
using System.Diagnostics;
using Paypal.Models;
using Paypal.Payloads;

namespace Paypal;

[TestFixture]
public class UserInfoTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up UserInfoTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed UserInfoTests fixture");
    }

    [Test]
    [Order(1)]
    public async Task GetUserInfo_ShouldReturn200()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var customerId = GetUserInfoPayload.GetCustomerId();
            var customerIdentityGuid = GetUserInfoPayload.GetCustomerIdentityGuid();

            TestReporter.AddStep(testName, "Sending authenticated GET request for customer PayPal user info");

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
                Endpoints.UserInfo(customerId, customerIdentityGuid),
                options);
            stopwatch.Stop();

            var responseText = await response.TextAsync();

            TestReporter.AddMetric(testName, "API Response Time (ms)", stopwatch.ElapsedMilliseconds);
            TestReporter.AddMetric(testName, "Response Status Code", response.Status);
            TestReporter.AddMetric(testName, "Response Size (bytes)", responseText.Length);

            Assert.That(response.Status, Is.EqualTo(200), "Expected successful response from customer PayPal user info endpoint");
            Assert.That(responseText, Is.Not.Empty, "Response body should not be empty");

            UserInfoResponse? userInfo;
            try
            {
                userInfo = JsonSerializer.Deserialize<UserInfoResponse>(responseText, JsonDefaults.Options);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Expected valid JSON response but parsing failed: {ex.Message}");
                return;
            }

            Assert.That(userInfo, Is.Not.Null, "Deserialized response should not be null");
            TestReporter.AddMetric(testName, "Customer ID", userInfo!.CustomerId ?? "N/A");
            TestReporter.AddMetric(testName, "Email", userInfo.Email ?? "N/A");
            TestReporter.AddStep(testName, "Validated successful 200 response with PaypalUserInfoResponse DTO");
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

