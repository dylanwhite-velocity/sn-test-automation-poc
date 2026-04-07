using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.Utilities;
using System.Diagnostics;

namespace ServiceNow.TestHelpers.Base;

/// <summary>
/// Base class for all ServiceNow integration UI test classes.
/// Mirrors the CUIT <c>UITestClassBase</c> pattern: provides centralized
/// <c>[TestInitialize]</c> / <c>[TestCleanup]</c> hooks, failure screenshot
/// capture, and trace-based logging.
///
/// <para>All team-specific test base classes should extend this class.</para>
/// </summary>
[TestClass]
public class ServiceNowTestClassBase
{
    /// <summary>MSTest context — injected automatically by the test runner.</summary>
    public TestContext? TestContext { get; set; }

    private FileStream? _logStream;
    private StreamWriter? _logWriter;

    /// <summary>
    /// Runs before each test method. Sets up trace logging to a per-test log file
    /// and captures the test start time.
    /// </summary>
    [TestInitialize]
    public void BaseTestInit()
    {
        if (TestContext == null) return;

        var testClassName = TestContext.FullyQualifiedTestName?.Split('.').LastOrDefault() ?? "Unknown";
        var logFileName = $"{testClassName}_{TestContext.TestName}.log";
        var logDir = TestContext.TestResultsDirectory ?? ".";

        try
        {
            var logPath = Path.Combine(logDir, logFileName);
            _logStream = new FileStream(logPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            _logWriter = new StreamWriter(_logStream) { AutoFlush = true };
            Trace.Listeners.Add(new TextWriterTraceListener(_logWriter));
            Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Test starting: {TestContext.TestName}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Warning: log file setup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs after each test method. Captures a failure screenshot if the test
    /// did not pass, then flushes and closes the trace log.
    /// </summary>
    [TestCleanup]
    public void BaseTestCleanup()
    {
        if (TestContext == null) return;

        if (TestContext.CurrentTestOutcome == UnitTestOutcome.Failed ||
            TestContext.CurrentTestOutcome == UnitTestOutcome.Timeout)
        {
            CaptureFailureScreenshot();
        }

        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Test finished: {TestContext.TestName} — {TestContext.CurrentTestOutcome}");

        try
        {
            _logWriter?.Flush();
            _logWriter?.Close();
            _logStream?.Close();
            Trace.Listeners.Clear();
        }
        catch
        {
            // Swallow — cleanup should not throw
        }
    }

    /// <summary>
    /// Captures a screenshot of the primary screen and attaches it to the test results.
    /// Called automatically on test failure; can also be invoked manually from test code.
    /// </summary>
    public void CaptureFailureScreenshot()
    {
        if (TestContext == null) return;

        var screenshotName = $"{TestContext.FullyQualifiedTestName?.Replace('.', '_')}_{TestContext.TestName}_Failure.png";
        var screenshotDir = TestContext.TestResultsDirectory ?? ".";
        var screenshotPath = Path.Combine(screenshotDir, screenshotName);

        try
        {
            ScreenCaptureUtils.CapturePrimaryScreen(screenshotPath);
            TestContext.AddResultFile(screenshotPath);
            TestContext.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Screenshot capture failed: {ex.Message}");
        }
    }
}
