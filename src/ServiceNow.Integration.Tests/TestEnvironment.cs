using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.Utilities;
using System.Net;

namespace ServiceNow.Integration.Tests;

/// <summary>
/// Assembly-level test environment setup and teardown.
/// Mirrors the CUIT <c>TestEnvironment</c> pattern.
///
/// <para>This class runs once per test assembly:
/// <list type="bullet">
///   <item><c>[AssemblyInitialize]</c> starts WinAppDriver before any tests run.</item>
///   <item><c>[AssemblyCleanup]</c> stops WinAppDriver after all tests complete.</item>
/// </list>
/// </para>
///
/// <para><strong>Important:</strong> This class must use <c>[TestClass]</c> (not
/// <c>[VideoLoggedTestClass]</c>) for <c>[AssemblyInitialize]</c> to be called.</para>
/// </summary>
[TestClass]
public class TestEnvironment
{
    /// <summary>Maximum time to wait for WinAppDriver to become responsive.</summary>
    private const int WadReadinessTimeoutMs = 15000;

    /// <summary>
    /// Runs once before any test in the assembly. Starts WinAppDriver, verifies
    /// it is accepting connections, and performs any one-time environment configuration.
    /// </summary>
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        testContext.WriteLine("=== ServiceNow Integration Tests — Assembly Initialize ===");

        // Start WinAppDriver programmatically (mirrors CUIT pattern)
        var wadStarted = WinAppDriverUtils.StartWinAppDriver();
        testContext.WriteLine(wadStarted
            ? "WinAppDriver started successfully."
            : "WARNING: WinAppDriver did not start. Tests requiring WAD will fail.");

        // Verify WinAppDriver is accepting connections before running tests
        if (wadStarted)
        {
            var wadReady = WaitForWinAppDriverReady(testContext);
            testContext.WriteLine(wadReady
                ? "WinAppDriver is accepting connections."
                : "WARNING: WinAppDriver readiness check timed out. First test may fail.");
        }

        testContext.WriteLine($"Machine: {Environment.MachineName}");
        testContext.WriteLine($"OS: {Environment.OSVersion}");
        testContext.WriteLine($"Test results directory: {testContext.TestResultsDirectory}");
    }

    /// <summary>
    /// Runs once after all tests in the assembly have completed.
    /// Stops WinAppDriver and cleans up resources.
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        // Kill any remaining Pro instances
        ApplicationUtils.KillArcGISProProcess();

        // Stop WinAppDriver
        WinAppDriverUtils.CloseWinAppDriver();
    }

    /// <summary>
    /// Polls the WinAppDriver <c>/status</c> endpoint until it responds,
    /// confirming the server is ready to accept session requests.
    /// </summary>
    /// <param name="testContext">Test context for logging.</param>
    /// <returns><c>true</c> if WinAppDriver responded within the timeout.</returns>
    private static bool WaitForWinAppDriverReady(TestContext testContext)
    {
        var statusUrl = $"{ApplicationUtils.DefaultWinAppDriverUrl}/status";

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var response = client.GetAsync(statusUrl).GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: WadReadinessTimeoutMs,
            delayBetweenAttemptsMs: 1000);
    }
}
