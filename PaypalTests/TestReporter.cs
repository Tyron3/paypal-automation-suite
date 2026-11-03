using System.Text.Json;
using System.Net;

namespace Paypal;

/// <summary>
/// Represents the result of a single test execution with detailed metrics and steps.
/// </summary>
public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Steps { get; set; } = new List<string>();
    public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

/// <summary>
/// A comprehensive test reporter that generates console, JSON, and HTML reports.
/// Supports tracking test execution, steps, metrics, and performance data.
/// Thread-safe for parallel test execution.
/// </summary>
public static class TestReporter
{
    private static readonly List<TestResult> _testResults = new List<TestResult>();
    private static readonly object _lock = new object();
    private static readonly string ReportsDir = Path.Combine(GetProjectRoot(), "TestReports");
    public static bool VerboseLogging { get; set; } = false; // Disable verbose logging for speed
    
    static TestReporter()
    {
        Directory.CreateDirectory(ReportsDir);
    }
    
    /// <summary>
    /// Finds the project root directory by looking for .csproj files.
    /// </summary>
    private static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectRoot = currentDirectory;
        
        // Navigate up to find the project root (look for .csproj file)
        while (!string.IsNullOrEmpty(projectRoot) && !Directory.GetFiles(projectRoot, "*.csproj").Any())
        {
            var parent = Directory.GetParent(projectRoot);
            if (parent == null) break;
            projectRoot = parent.FullName;
        }
        
        return projectRoot;
    }
    
    /// <summary>
    /// Resets the test reporter by clearing all test results.
    /// This should be called at the beginning of a new test session.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _testResults.Clear();
        }
        if (VerboseLogging)
            Console.WriteLine("🔄 Test reporter reset - cleared all previous test results");
    }
    
    /// <summary>
    /// Starts tracking a new test execution.
    /// </summary>
    /// <param name="testName">Name of the test being started</param>
    public static void StartTest(string testName)
    {
        var result = new TestResult
        {
            TestName = testName,
            StartTime = DateTime.UtcNow,
            Status = "Running"
        };
        lock (_lock)
        {
            _testResults.Add(result);
        }
        if (VerboseLogging)
            Console.WriteLine($"🚀 Starting test: {testName} at {result.StartTime:yyyy-MM-dd HH:mm:ss UTC}");
    }
    
    /// <summary>
    /// Ends a test execution and records the final status.
    /// </summary>
    /// <param name="testName">Name of the test being ended</param>
    /// <param name="passed">Whether the test passed or failed</param>
    /// <param name="errorMessage">Optional error message if test failed</param>
    public static void EndTest(string testName, bool passed, string? errorMessage = null)
    {
        lock (_lock)
        {
            var result = _testResults.FirstOrDefault(r => r.TestName == testName);
            if (result != null)
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.Status = passed ? "Passed" : "Failed";
                result.ErrorMessage = errorMessage;
                
                if (VerboseLogging)
                {
                    var statusEmoji = passed ? "✅" : "❌";
                    Console.WriteLine($"{statusEmoji} Test {testName} {result.Status} in {result.Duration.TotalSeconds:F2}s");
                    if (!passed && !string.IsNullOrEmpty(errorMessage))
                    {
                        Console.WriteLine($"   Error: {errorMessage}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Adds a step to the test execution log.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="step">Description of the step being performed</param>
    public static void AddStep(string testName, string step)
    {
        lock (_lock)
        {
            var result = _testResults.FirstOrDefault(r => r.TestName == testName);
            result?.Steps.Add($"{DateTime.UtcNow:HH:mm:ss} - {step}");
        }
        if (VerboseLogging)
            Console.WriteLine($"   📝 {step}");
    }
    
    /// <summary>
    /// Adds a custom metric to the test result.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Value of the metric</param>
    public static void AddMetric(string testName, string metricName, object value)
    {
        lock (_lock)
        {
            var result = _testResults.FirstOrDefault(r => r.TestName == testName);
            if (result != null)
            {
                result.Metrics[metricName] = value;
            }
        }
        if (VerboseLogging)
            Console.WriteLine($"   📊 {metricName}: {value}");
    }
    
    /// <summary>
    /// Generates all report types (console, JSON, and HTML).
    /// </summary>
    public static void GenerateReports()
    {
        GenerateConsoleReport();
        GenerateJsonReport();
        GenerateHtmlReport();
    }
    
    /// <summary>
    /// Generates a detailed console report with summary and performance metrics.
    /// </summary>
    public static void GenerateConsoleReport()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("📊 TEST EXECUTION SUMMARY");
        Console.WriteLine(new string('=', 80));
        
        var passed = _testResults.Count(r => r.Status == "Passed");
        var failed = _testResults.Count(r => r.Status == "Failed");
        var total = _testResults.Count;
        var totalDuration = TimeSpan.FromTicks(_testResults.Sum(r => r.Duration.Ticks));
        
        Console.WriteLine($"Total Tests: {total}");
        Console.WriteLine($"✅ Passed: {passed}");
        Console.WriteLine($"❌ Failed: {failed}");
        Console.WriteLine($"⏱️  Total Duration: {totalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"📈 Success Rate: {(total > 0 ? (passed * 100.0 / total) : 0):F1}%");
        
        if (failed > 0)
        {
            Console.WriteLine("\n❌ FAILED TESTS:");
            foreach (var failedTest in _testResults.Where(r => r.Status == "Failed"))
            {
                Console.WriteLine($"   • {failedTest.TestName}: {failedTest.ErrorMessage}");
            }
        }
        
        Console.WriteLine("\n⏱️  PERFORMANCE METRICS:");
        foreach (var test in _testResults.OrderByDescending(r => r.Duration))
        {
            Console.WriteLine($"   • {test.TestName}: {test.Duration.TotalSeconds:F2}s");
        }
    }
    
    /// <summary>
    /// Generates a JSON report file with detailed test results.
    /// </summary>
    public static void GenerateJsonReport()
    {
        var reportPath = Path.Combine(ReportsDir, $"test-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        var jsonReport = new
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalTests = _testResults.Count,
                Passed = _testResults.Count(r => r.Status == "Passed"),
                Failed = _testResults.Count(r => r.Status == "Failed"),
                TotalDuration = _testResults.Sum(r => r.Duration.TotalSeconds),
                SuccessRate = _testResults.Count > 0 ? (_testResults.Count(r => r.Status == "Passed") * 100.0 / _testResults.Count) : 0
            },
            Tests = _testResults
        };
        
        File.WriteAllText(reportPath, JsonSerializer.Serialize(jsonReport, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"📄 JSON report saved: {reportPath}");
    }
    
    /// <summary>
    /// Generates an HTML report file with styled test results.
    /// </summary>
    public static void GenerateHtmlReport()
    {
        var reportPath = Path.Combine(ReportsDir, $"test-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");
        
        var passed = _testResults.Count(r => r.Status == "Passed");
        var failed = _testResults.Count(r => r.Status == "Failed");
        var total = _testResults.Count;
        var totalDuration = _testResults.Sum(r => r.Duration.TotalSeconds);
        var successRate = total > 0 ? (passed * 100.0 / total) : 0;
        
        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Paypal Test Report - {DateTime.UtcNow:yyyy-MM-dd}</title>
    <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap"" rel=""stylesheet"">
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css"" rel=""stylesheet"">
    <style>
        :root {{
            --primary-color: #2563eb;
            --success-color: #10b981;
            --error-color: #ef4444;
            --warning-color: #f59e0b;
            --bg-primary: #0f172a;
            --bg-secondary: #f8fafc;
            --bg-card: #ffffff;
            --text-primary: #1e293b;
            --text-secondary: #64748b;
            --border-color: #e2e8f0;
            --shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
            --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
        }}

        @media (prefers-color-scheme: dark) {{
            :root {{
                --bg-primary: #0f172a;
                --bg-secondary: #1e293b;
                --bg-card: #334155;
                --text-primary: #f1f5f9;
                --text-secondary: #94a3b8;
                --border-color: #475569;
            }}
        }}

        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            background: var(--bg-primary);
            color: var(--text-primary);
            line-height: 1.6;
            transition: all 0.3s ease;
        }}

        .container {{
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }}

        .header {{
            text-align: center;
            margin-bottom: 3rem;
            padding: 2rem;
            background-image: url('../mukuru-card.jpg');  
            background-size: cover;
            background-repeat: no-repeat;
            background-position: center center;
            border-radius: 1rem;
            box-shadow: var(--shadow-lg);
            min-height: 120px;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            position: relative;
        }}

        .header::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.6);
            border-radius: 1rem;
            z-index: 1;
        }}

        .header h1,
        .header .subtitle {{
            position: relative;
            z-index: 2;
            color: white;
            text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
            font-weight: 700;
        }}

        .header h1 {{
            font-size: 2.5rem;
            margin-bottom: 0.5rem;
            text-align: center;
        }}

        .header .subtitle {{
            font-size: 1.1rem;
            opacity: 0.95;
            text-align: center;
        }}

        .metrics-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 1.5rem;
            margin-bottom: 3rem;
        }}

        .metric-card {{
            background: var(--bg-card);
            padding: 2rem;
            border-radius: 1rem;
            box-shadow: var(--shadow);
            border: 1px solid var(--border-color);
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }}

        .metric-card:hover {{
            transform: translateY(-2px);
            box-shadow: var(--shadow-lg);
        }}

        .metric-header {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 1rem;
        }}

        .metric-icon {{
            width: 48px;
            height: 48px;
            border-radius: 0.75rem;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            color: white;
        }}

        .metric-icon.success {{ background: var(--success-color); }}
        .metric-icon.error {{ background: var(--error-color); }}
        .metric-icon.total {{ background: var(--primary-color); }}
        .metric-icon.time {{ background: var(--warning-color); }}

        .metric-value {{
            font-size: 2.5rem;
            font-weight: 700;
            line-height: 1;
        }}

        .metric-label {{
            color: var(--text-secondary);
            font-size: 0.875rem;
            font-weight: 500;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}

        .success-rate {{
            margin-top: 1rem;
            padding: 1rem;
            background: var(--bg-secondary);
            border-radius: 0.5rem;
        }}

        .progress-bar {{
            height: 8px;
            background: var(--border-color);
            border-radius: 4px;
            overflow: hidden;
            margin-top: 0.5rem;
        }}

        .progress-fill {{
            height: 100%;
            background: linear-gradient(90deg, var(--success-color), #34d399);
            border-radius: 4px;
            transition: width 0.8s ease;
        }}

        .tests-section {{
            background: var(--bg-card);
            border-radius: 1rem;
            box-shadow: var(--shadow);
            border: 1px solid var(--border-color);
            overflow: hidden;
        }}

        .section-header {{
            padding: 1.5rem 2rem;
            background: var(--bg-secondary);
            border-bottom: 1px solid var(--border-color);
            display: flex;
            align-items: center;
            justify-content: space-between;
        }}

        .section-title {{
            font-size: 1.25rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }}

        .filter-buttons {{
            display: flex;
            gap: 0.5rem;
        }}

        .filter-btn {{
            padding: 0.5rem 1rem;
            border: 1px solid var(--border-color);
            background: var(--bg-card);
            color: var(--text-primary);
            border-radius: 0.5rem;
            cursor: pointer;
            font-size: 0.875rem;
            transition: all 0.2s ease;
        }}

        .filter-btn:hover,
        .filter-btn.active {{
            background: var(--primary-color);
            color: white;
            border-color: var(--primary-color);
        }}

        .test-item {{
            padding: 1.5rem 2rem;
            border-bottom: 1px solid var(--border-color);
            transition: background 0.2s ease;
        }}

        .test-item:last-child {{
            border-bottom: none;
        }}

        .test-item:hover {{
            background: var(--bg-secondary);
        }}

        .test-header {{
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 1rem;
        }}

        .test-name {{
            font-weight: 600;
            font-size: 1.1rem;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }}

        .status-badge {{
            padding: 0.25rem 0.75rem;
            border-radius: 9999px;
            font-size: 0.75rem;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}

        .status-badge.passed {{
            background: rgba(16, 185, 129, 0.1);
            color: var(--success-color);
        }}

        .status-badge.failed {{
            background: rgba(239, 68, 68, 0.1);
            color: var(--error-color);
        }}

        .test-duration {{
            color: var(--text-secondary);
            font-size: 0.875rem;
            display: flex;
            align-items: center;
            gap: 0.25rem;
        }}

        .test-details {{
            display: none;
            margin-top: 1rem;
            padding-top: 1rem;
            border-top: 1px solid var(--border-color);
        }}

        .test-details.expanded {{
            display: block;
        }}

        .details-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 2rem;
        }}

        .steps-list {{
            list-style: none;
        }}

        .steps-list li {{
            padding: 0.5rem 0;
            border-bottom: 1px solid var(--border-color);
            font-size: 0.875rem;
            color: var(--text-secondary);
        }}

        .metrics-list {{
            display: grid;
            gap: 0.5rem;
        }}

        .metric-item {{
            display: flex;
            justify-content: space-between;
            padding: 0.5rem;
            background: var(--bg-secondary);
            border-radius: 0.5rem;
            font-size: 0.875rem;
        }}

        .error-message {{
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.2);
            color: var(--error-color);
            padding: 1rem;
            border-radius: 0.5rem;
            margin-top: 1rem;
            font-family: 'Monaco', 'Menlo', monospace;
            font-size: 0.875rem;
        }}

        .expand-btn {{
            background: none;
            border: none;
            color: var(--primary-color);
            cursor: pointer;
            padding: 0.5rem;
            border-radius: 0.5rem;
            transition: all 0.2s ease;
        }}

        .expand-btn:hover {{
            background: rgba(37, 99, 235, 0.1);
        }}

        .footer {{
            text-align: center;
            margin-top: 3rem;
            padding: 2rem;
            color: var(--text-secondary);
            font-size: 0.875rem;
        }}

        @media (max-width: 768px) {{
            .container {{
                padding: 1rem;
            }}
            
            .details-grid {{
                grid-template-columns: 1fr;
            }}
            
            .test-header {{
                flex-direction: column;
                align-items: flex-start;
                gap: 0.5rem;
            }}
            
            .header h1 {{
                font-size: 2rem;
            }}
        }}

        .fade-in {{
            animation: fadeIn 0.6s ease-out;
        }}

        @keyframes fadeIn {{
            from {{
                opacity: 0;
                transform: translateY(20px);
            }}
            to {{
                opacity: 1;
                transform: translateY(0);
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <header class=""header fade-in"">
            <h1><i class=""fas fa-flask""></i> PayPal Test Report</h1>
            <p class=""subtitle"">Generated on {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC</p>
        </header>

        <div class=""metrics-grid fade-in"">
            <div class=""metric-card"">
                <div class=""metric-header"">
                    <div class=""metric-icon total"">
                        <i class=""fas fa-list""></i>
                    </div>
                </div>
                <div class=""metric-value"">{total}</div>
                <div class=""metric-label"">Total Tests</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-header"">
                    <div class=""metric-icon success"">
                        <i class=""fas fa-check""></i>
                    </div>
                </div>
                <div class=""metric-value"">{passed}</div>
                <div class=""metric-label"">Passed</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-header"">
                    <div class=""metric-icon error"">
                        <i class=""fas fa-times""></i>
                    </div>
                </div>
                <div class=""metric-value"">{failed}</div>
                <div class=""metric-label"">Failed</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-header"">
                    <div class=""metric-icon time"">
                        <i class=""fas fa-clock""></i>
                    </div>
                </div>
                <div class=""metric-value"">{totalDuration:F1}s</div>
                <div class=""metric-label"">Total Duration</div>
                <div class=""success-rate"">
                    <div style=""display: flex; justify-content: space-between; align-items: center;"">
                        <span>Success Rate</span>
                        <span style=""font-weight: 600;"">{successRate:F1}%</span>
                    </div>
                    <div class=""progress-bar"">
                        <div class=""progress-fill"" style=""width: {successRate}%""></div>
                    </div>
                </div>
            </div>
        </div>

        <div class=""tests-section fade-in"">
            <div class=""section-header"">
                <h2 class=""section-title"">
                    <i class=""fas fa-list-check""></i>
                    Test Results
                </h2>
                <div class=""filter-buttons"">
                    <button class=""filter-btn active"" onclick=""filterTests('all')"">All</button>
                    <button class=""filter-btn"" onclick=""filterTests('passed')"">Passed</button>
                    <button class=""filter-btn"" onclick=""filterTests('failed')"">Failed</button>
                </div>
            </div>
            <div class=""tests-list"">";

        foreach (var test in _testResults.OrderBy(r => r.StartTime))
        {
            var statusClass = test.Status.ToLower();
            var statusIcon = test.Status == "Passed" ? "fas fa-check-circle" : "fas fa-times-circle";
            var testId = WebUtility.HtmlEncode(test.TestName.Replace(" ", "").Replace(".", ""));
            var encodedTestName = WebUtility.HtmlEncode(test.TestName);

            html += $@"
                <div class=""test-item"" data-status=""{statusClass}"">
                    <div class=""test-header"">
                        <div class=""test-name"">
                            <i class=""{statusIcon}"" style=""color: var(--{(test.Status == "Passed" ? "success" : "error")}-color);""></i>
                            {encodedTestName}
                            <span class=""status-badge {statusClass}"">{test.Status}</span>
                        </div>
                        <div style=""display: flex; align-items: center; gap: 1rem;"">
                            <div class=""test-duration"">
                                <i class=""fas fa-stopwatch""></i>
                                {test.Duration.TotalSeconds:F2}s
                            </div>
                            <button class=""expand-btn"" onclick=""toggleDetails('{testId}')"">
                                <i class=""fas fa-chevron-down"" id=""icon-{testId}""></i>
                            </button>
                        </div>
                    </div>";

            if (!string.IsNullOrEmpty(test.ErrorMessage))
            {
                html += $@"
                    <div class=""error-message"">
                        <strong>Error:</strong> {WebUtility.HtmlEncode(test.ErrorMessage)}
                    </div>";
            }

            html += $@"
                    <div class=""test-details"" id=""details-{testId}"">
                        <div class=""details-grid"">
                            <div>
                                <h4 style=""margin-bottom: 1rem; color: var(--text-primary);"">
                                    <i class=""fas fa-list-ol""></i> Test Steps
                                </h4>
                                <ul class=""steps-list"">";

            foreach (var step in test.Steps)
            {
                html += $"<li>{WebUtility.HtmlEncode(step)}</li>";
            }

            html += $@"
                                </ul>
                            </div>
                            <div>
                                <h4 style=""margin-bottom: 1rem; color: var(--text-primary);"">
                                    <i class=""fas fa-chart-line""></i> Metrics
                                </h4>
                                <div class=""metrics-list"">";

            foreach (var metric in test.Metrics)
            {
                html += $@"
                                    <div class=""metric-item"">
                                        <span>{WebUtility.HtmlEncode(metric.Key)}</span>
                                        <span style=""font-weight: 600;"">{WebUtility.HtmlEncode(metric.Value?.ToString() ?? "")}</span>
                                    </div>";
            }

            html += $@"
                                    <div class=""metric-item"">
                                        <span>Start Time</span>
                                        <span style=""font-weight: 600;"">{test.StartTime:HH:mm:ss}</span>
                                    </div>
                                    <div class=""metric-item"">
                                        <span>End Time</span>
                                        <span style=""font-weight: 600;"">{test.EndTime:HH:mm:ss}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>";
        }

        html += $@"
            </div>
        </div>

        <footer class=""footer"">
            <p>Generated by Mukuru Test Automation Framework</p>
            <p>Report created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </footer>
    </div>

    <script>
        function toggleDetails(testId) {{
            const details = document.getElementById('details-' + testId);
            const icon = document.getElementById('icon-' + testId);
            
            if (details.classList.contains('expanded')) {{
                details.classList.remove('expanded');
                icon.className = 'fas fa-chevron-down';
            }} else {{
                details.classList.add('expanded');
                icon.className = 'fas fa-chevron-up';
            }}
        }}

        function filterTests(status) {{
            const tests = document.querySelectorAll('.test-item');
            const buttons = document.querySelectorAll('.filter-btn');
            
            // Update active button
            buttons.forEach(btn => btn.classList.remove('active'));
            event.target.classList.add('active');
            
            // Filter tests
            tests.forEach(test => {{
                if (status === 'all' || test.dataset.status === status) {{
                    test.style.display = 'block';
                }} else {{
                    test.style.display = 'none';
                }}
            }});
        }}

        // Add fade-in animation to elements as they come into view
        const observerOptions = {{
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        }};

        const observer = new IntersectionObserver((entries) => {{
            entries.forEach(entry => {{
                if (entry.isIntersecting) {{
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }}
            }});
        }}, observerOptions);

        document.querySelectorAll('.test-item').forEach(item => {{
            item.style.opacity = '0';
            item.style.transform = 'translateY(20px)';
            item.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            observer.observe(item);
        }});

        // Auto-expand failed tests
        document.addEventListener('DOMContentLoaded', () => {{
            const failedTests = document.querySelectorAll('.test-item[data-status=""failed""]');
            failedTests.forEach(test => {{
                const testName = test.querySelector('.test-name').textContent.trim().replace(/\s+/g, '').replace(/\./g, '');
                const details = document.getElementById('details-' + testName);
                const icon = document.getElementById('icon-' + testName);
                if (details && icon) {{
                    details.classList.add('expanded');
                    icon.className = 'fas fa-chevron-up';
                }}
            }});
        }});
    </script>
</body>
</html>";

        File.WriteAllText(reportPath, html);
        Console.WriteLine($"🌐 HTML report saved: {reportPath}");
    }
}