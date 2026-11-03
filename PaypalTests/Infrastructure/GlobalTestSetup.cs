
namespace Paypal;

[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void GlobalSetUp()
    {
        TestReporter.Reset();
        Console.WriteLine("🚀 Starting global test session");
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        TestReporter.GenerateReports();
        Console.WriteLine("📊 Global test session completed - reports generated");
    }
}

