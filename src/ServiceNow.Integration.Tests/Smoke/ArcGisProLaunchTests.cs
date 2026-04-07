using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServiceNow.Integration.Tests.Smoke;

/// <summary>
/// Smoke tests that verify the WinAppDriver → ArcGIS Pro launch chain works.
/// These are the first tests to run and validate the basic test infrastructure.
///
/// <para>Proves that:
/// <list type="number">
///   <item>WinAppDriver can launch ArcGIS Pro via Appium capabilities.</item>
///   <item>The ArcGIS Pro main window is reachable from the driver session.</item>
///   <item>The POM layer (Application) correctly wraps the Pro instance.</item>
///   <item>MSTest v2 reports results correctly via dotnet test.</item>
/// </list>
/// </para>
///
/// <para><strong>Prerequisites:</strong> Windows, ArcGIS Pro installed, WinAppDriver installed
/// (started automatically by <see cref="TestEnvironment"/>).</para>
/// </summary>
[TestClass]
public class ArcGisProLaunchTests : ServiceNowTestBase
{
    /// <summary>
    /// Verifies that ArcGIS Pro launches and the main window title contains "ArcGIS Pro".
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    [Description("Verify ArcGIS Pro launches via WinAppDriver and the main window is accessible")]
    public void VerifyArcGisProLaunches()
    {
        // Arrange & Act — launch Pro (no project, opens Start Page)
        var app = StartProWithProject();

        // Assert — application POM created successfully
        Assert.IsNotNull(app, "Application POM should be created");
        Assert.IsNotNull(app.MainWindow, "ArcGIS Pro main window should be found");

        var title = app.GetWindowTitle();
        Assert.IsTrue(
            title.Contains("ArcGIS Pro", StringComparison.OrdinalIgnoreCase),
            $"Window title should contain 'ArcGIS Pro', got: '{title}'");
    }

    /// <summary>
    /// Verifies that the ArcGIS Pro main window has child elements, confirming
    /// the UI element tree is accessible through WinAppDriver.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    [Description("Verify the ArcGIS Pro UI element tree is accessible via WinAppDriver")]
    public void VerifyMainWindowHasElements()
    {
        // Arrange & Act
        var app = StartProWithProject();

        // Assert — UI tree is navigable
        var children = app.MainWindow.FindElementsByXPath("*");
        Assert.IsTrue(
            children.Count > 0,
            "ArcGIS Pro main window should have child UI elements");
    }

    /// <summary>
    /// Verifies that the application responds to readiness checks,
    /// confirming Pro has finished initializing.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    [Description("Verify ArcGIS Pro is responsive after launch")]
    public void VerifyApplicationIsResponsive()
    {
        // Arrange & Act
        var app = StartProWithProject();

        // Assert — Pro responds to attribute queries
        var isReady = app.WaitForAppReady(timeoutMs: 10000);
        Assert.IsTrue(isReady, "ArcGIS Pro should be responsive within 10 seconds of startup wait");
    }
}
