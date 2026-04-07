using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.Utilities;

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
    /// <summary>
    /// Runs once before any test in the assembly. Starts WinAppDriver and
    /// performs any one-time environment configuration.
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
}
