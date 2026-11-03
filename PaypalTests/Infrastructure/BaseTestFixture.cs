using NUnit.Framework.Interfaces;
using Microsoft.Playwright;
using dotenv.net;
using static System.Environment;
using System.Text.Json;
using System.Diagnostics;

namespace Paypal;

/// <summary>
/// Base class for all test fixtures to eliminate code duplication in SetUp/TearDown
/// </summary>
public abstract class BaseTestFixture
{
    protected string _accessToken = "";
    protected string _payinrefToken = "";
    protected IPlaywright? _playwright;
    protected IAPIRequestContext? _requestContext;
    protected IAPIRequestContext? _payinRefRequestContext;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        try
        {
            // Load environment variables once
            DotEnv.Load(options: new DotEnvOptions(
                envFilePaths: new[] { "../../../.env" }
            ));

            // Initialize Playwright once per fixture
            _playwright = await Playwright.CreateAsync();

            var baseUrl = GetEnvironmentVariable("BASEURL");
            var payinRefBaseUrl = GetEnvironmentVariable("PAYINREFBASEURL");

            // Get tokens once per fixture
            var tokenStopwatch = Stopwatch.StartNew();
            _accessToken = await PaymentTestHelpers.GetAccessTokenAsync("CLIENTID", "CLIENTSECRET", "ACCESSTOKEN", "SCOPE");
            _payinrefToken = await PaymentTestHelpers.GetAccessTokenAsync("PAYINREFCLIENTID", "PAYINREFCLIENTSECRET", "PAYINREFACCESSTOKEN");
            tokenStopwatch.Stop();
            Console.WriteLine($"Tokens retrieved in {tokenStopwatch.ElapsedMilliseconds}ms");

            _requestContext = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
            {
                BaseURL = baseUrl
            });

            _payinRefRequestContext = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
            {
                BaseURL = payinRefBaseUrl
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OneTimeSetUp failed: {ex.Message}");
            throw;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_requestContext != null)
            await _requestContext.DisposeAsync();
        if (_payinRefRequestContext != null)
            await _payinRefRequestContext.DisposeAsync();
        _playwright?.Dispose();
    }
    
    // Helper method to create authenticated requests
    protected async Task<IAPIResponse> MakeAuthenticatedRequestAsync(
        string endpoint,
        HttpMethod method,
        object? payload = null, 
        bool usePayinRefContext = false,
        Dictionary<string, string>? extraHeaders = null)
    {
        var context = usePayinRefContext ? _payinRefRequestContext : _requestContext;
        var token = usePayinRefContext ? _payinrefToken : _accessToken;
        
        if (context == null)
            throw new InvalidOperationException("Request context not initialized");

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {token}" },
            { "Content-Type", "application/json" }
        };

        if (extraHeaders != null)
        {
            foreach (var header in extraHeaders)
                headers[header.Key] = header.Value;
        }

        var options = new APIRequestContextOptions
        {
            Headers = headers
        };

        if (payload != null)
            options.Data = JsonSerializer.Serialize(payload, JsonDefaults.Options);

        return method.Method.ToUpper() switch
        {
            "GET" => await context.GetAsync(endpoint, options),
            "POST" => await context.PostAsync(endpoint, options),
            "PUT" => await context.PutAsync(endpoint, options),
            "PATCH" => await context.PatchAsync(endpoint, options),
            "DELETE" => await context.DeleteAsync(endpoint, options),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };
    }
    
    [SetUp]
    public void Setup()
    {
        try
        {
            var testName = TestContext.CurrentContext.Test.Name;
            TestReporter.StartTest(testName); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load .env file: {ex.Message}");
            throw;
        }
    }

    [TearDown]
    public void TearDown()
    {
        var testName = TestContext.CurrentContext.Test.Name;
        var outcome = TestContext.CurrentContext.Result.Outcome;
        var passed = outcome == ResultState.Success;
        var errorMessage = outcome == ResultState.Failure ? TestContext.CurrentContext.Result.Message : null;

        TestReporter.EndTest(testName, passed, errorMessage);
    }
}


