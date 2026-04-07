using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using ServiceNow.TestHelpers.Base;
using ServiceNow.TestHelpers.ProApplication;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.Integration.Tests;

/// <summary>
/// Team-specific test base class for ServiceNow integration tests.
/// Extends <see cref="ServiceNowTestClassBase"/> with per-test Pro lifecycle management.
///
/// <para>Mirrors the CUIT team test base pattern (e.g., <c>GeoprocessingTestBase</c>):
/// <list type="bullet">
///   <item>Kills any existing ArcGIS Pro processes before each test.</item>
///   <item>Provides <see cref="StartProWithProject"/> to launch Pro with a specific project.</item>
///   <item>Closes Pro after each test.</item>
/// </list>
/// </para>
///
/// <para>All CDF and ILL test classes should extend this class.</para>
/// </summary>
[TestClass]
public class ServiceNowTestBase : ServiceNowTestClassBase
{
    /// <summary>The ArcGIS Pro Application POM for the current test.</summary>
    protected Application? Application { get; set; }

    /// <summary>The WinAppDriver session for the current test.</summary>
    protected WindowsDriver<AppiumWebElement>? Driver { get; set; }

    /// <summary>
    /// ArcGIS Pro executable path. Read from test.runsettings <c>ArcGISProPath</c> parameter,
    /// or falls back to the default install location.
    /// </summary>
    protected string ArcGISProPath =>
        TestContext?.Properties["ArcGISProPath"]?.ToString()
        ?? @"C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe";

    /// <summary>
    /// WinAppDriver URL. Read from test.runsettings <c>WinAppDriverUrl</c> parameter,
    /// or falls back to the default.
    /// </summary>
    protected string WinAppDriverUrl =>
        TestContext?.Properties["WinAppDriverUrl"]?.ToString()
        ?? ApplicationUtils.DefaultWinAppDriverUrl;

    /// <summary>
    /// Seconds to wait for ArcGIS Pro to initialize after launch.
    /// Read from test.runsettings <c>StartupWaitSeconds</c> parameter.
    /// </summary>
    protected int StartupWaitSeconds =>
        int.TryParse(TestContext?.Properties["StartupWaitSeconds"]?.ToString(), out var seconds)
            ? seconds
            : 45;

    /// <summary>
    /// Runs before each test. Kills any existing Pro processes to ensure a clean state.
    /// </summary>
    [TestInitialize]
    public void ServiceNowTestInit()
    {
        if (ApplicationUtils.GetArcGISProCount() > 0)
        {
            ApplicationUtils.KillArcGISProProcess();
            WaitingUtils.Wait(2000);
        }
    }

    /// <summary>
    /// Runs after each test. Closes ArcGIS Pro if still running.
    /// </summary>
    [TestCleanup]
    public void ServiceNowTestCleanup()
    {
        try
        {
            Application?.CloseApplication();
        }
        catch
        {
            // Best effort
        }

        if (ApplicationUtils.GetArcGISProCount() > 0)
        {
            ApplicationUtils.KillArcGISProProcess();
        }

        Driver = null;
        Application = null;
    }

    /// <summary>
    /// Launches ArcGIS Pro with an optional project file (.aprx) and returns the
    /// <see cref="Application"/> POM.
    /// </summary>
    /// <param name="projectPath">
    /// Optional full path to a .aprx project file. If null, Pro opens to the Start Page.
    /// </param>
    /// <returns>An <see cref="Application"/> POM wrapping the launched Pro instance.</returns>
    protected Application StartProWithProject(string? projectPath = null)
    {
        Driver = ApplicationUtils.StartApplicationWAD(
            proExePath: ArcGISProPath,
            winAppDriverUrl: WinAppDriverUrl,
            commandLineArgs: projectPath);

        // ArcGIS Pro takes significant time to initialize
        TestContext?.WriteLine($"Waiting {StartupWaitSeconds}s for ArcGIS Pro to initialize...");
        WaitingUtils.Wait(StartupWaitSeconds * 1000);

        Application = new Application(Driver);
        return Application;
    }

    /// <summary>
    /// Attaches to an already-running ArcGIS Pro instance and returns the
    /// <see cref="Application"/> POM. Useful for continuous test scenarios.
    /// </summary>
    /// <returns>An <see cref="Application"/> POM wrapping the existing Pro instance.</returns>
    protected Application AttachToExistingPro()
    {
        Driver = ApplicationUtils.GetExistingDesktopSession(WinAppDriverUrl);
        Application = new Application(Driver);
        return Application;
    }
}
