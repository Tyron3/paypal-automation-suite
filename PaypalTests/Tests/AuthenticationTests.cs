using System.Diagnostics;

namespace Paypal;

[TestFixture]
public class AuthenticationTests : BaseTestFixture
{
    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        Console.WriteLine("🔧 Setting up AuthenticationTests fixture");
    }

    [OneTimeTearDown]
    public new void OneTimeTearDown()
    {
        Console.WriteLine("🏁 Completed AuthenticationTests fixture");
    }
    
    [Test]
    [Order(1)]
    public async Task GetTwoAccessTokensTest()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            
            TestReporter.AddStep(testName, "Getting first access token with primary client");
            var stopwatch = Stopwatch.StartNew();
            var valtariToken = await PaymentTestHelpers.GetAccessTokenAsync("CLIENTID", "CLIENTSECRET", "ACCESSTOKEN", "SCOPE");
            stopwatch.Stop();
            TestReporter.AddMetric(testName, "First Token Request Time (ms)", stopwatch.ElapsedMilliseconds);
            
            Assert.That(valtariToken, Is.Not.Null, "First access token should not be null");
            TestReporter.AddStep(testName, "First access token retrieved successfully");
            TestReporter.AddStep(testName, "Getting second access token with secondary client");
            stopwatch.Restart();
            
            var payinrefToken = await PaymentTestHelpers.GetAccessTokenAsync("PAYINREFCLIENTID", "PAYINREFCLIENTSECRET", "PAYINREFACCESSTOKEN");
            stopwatch.Stop();
            TestReporter.AddMetric(testName, "Second Token Request Time (ms)", stopwatch.ElapsedMilliseconds);
            
            Assert.That(payinrefToken, Is.Not.Null, "Second access token should not be null");
            TestReporter.AddStep(testName, "Second access token retrieved successfully");
            TestReporter.AddMetric(testName, "First Token Length", valtariToken.Length);
            TestReporter.AddMetric(testName, "Second Token Length", payinrefToken.Length);
            
            Console.WriteLine($"Valtari Token: {valtariToken}");
            Console.WriteLine($"Payin Reference Token: {payinrefToken}");
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

