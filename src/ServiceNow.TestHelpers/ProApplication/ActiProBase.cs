using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication;

/// <summary>
/// Root Page Object Model base class. Mirrors the CUIT <c>ActiProBase</c> pattern.
/// Holds the <see cref="WinAppDriver"/> session and a reference to the
/// ArcGIS Pro <see cref="MainWindow"/> element.
///
/// <para>All POM classes that represent parts of the ArcGIS Pro UI should inherit
/// from this class (directly or indirectly through <see cref="Application"/>).</para>
/// </summary>
public abstract class ActiProBase
{
    /// <summary>
    /// AutomationId of the ArcGIS Pro main window.
    /// Discovered via Inspect.exe — stable across Pro 3.x versions.
    /// </summary>
    public const string MainWindowAutomationId = "ArcGISProMainWindow";

    /// <summary>
    /// Creates a new POM base, finding the ArcGIS Pro main window from the driver session.
    /// </summary>
    /// <param name="winAppDriver">An active WinAppDriver session.</param>
    protected ActiProBase(WindowsDriver<AppiumWebElement> winAppDriver)
    {
        WinAppDriver = winAppDriver;

        MainWindow = WaitingUtils.RetryAssignmentUntilSuccess(
            () => WinAppDriver.FindElementByAccessibilityId(MainWindowAutomationId),
            timeoutMs: 30000,
            delayBetweenAttemptsMs: 500,
            debugInfo: "ActiProBase constructor — searching for ArcGISProMainWindow")
            ?? throw new InvalidOperationException(
                $"Could not find ArcGIS Pro main window (AutomationId: {MainWindowAutomationId})");
    }

    /// <summary>The active WinAppDriver session.</summary>
    public WindowsDriver<AppiumWebElement> WinAppDriver { get; }

    /// <summary>The ArcGIS Pro main window element.</summary>
    public AppiumWebElement MainWindow { get; }
}
