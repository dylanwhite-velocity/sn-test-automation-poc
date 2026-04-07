using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication;

/// <summary>
/// Page Object representing a running ArcGIS Pro instance.
/// Mirrors the CUIT <c>ProApplication.Application</c> class.
///
/// <para>Constructed with an active <see cref="WindowsDriver{AppiumWebElement}"/> session.
/// Provides high-level methods for interacting with the Pro application window
/// and acts as the root from which ribbon tabs, panes, and dialogs are accessed.</para>
/// </summary>
public class Application : ActiProBase
{
    /// <summary>
    /// Creates a new Application POM wrapping an active WinAppDriver session.
    /// </summary>
    /// <param name="winAppDriver">An active WinAppDriver session targeting ArcGIS Pro.</param>
    public Application(WindowsDriver<AppiumWebElement> winAppDriver) : base(winAppDriver)
    {
    }

    /// <summary>
    /// Returns <c>true</c> if a "Yes/No/OK/Cancel" confirmation dialog is visible.
    /// </summary>
    public bool DoesConfirmationDialogExist()
    {
        return MainWindow.FindElementsByClassName("Window").Any();
    }

    /// <summary>
    /// Waits for ArcGIS Pro to become responsive after an operation.
    /// Checks that the main window is still accessible.
    /// </summary>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns><c>true</c> if Pro is responsive within the timeout.</returns>
    public bool WaitForAppReady(int timeoutMs = 30000)
    {
        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    _ = MainWindow.GetAttribute("Name");
                    return true;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: timeoutMs);
    }

    /// <summary>
    /// Gets the title of the ArcGIS Pro main window.
    /// </summary>
    public string GetWindowTitle()
    {
        return MainWindow.GetAttribute("Name") ?? string.Empty;
    }

    /// <summary>
    /// Closes ArcGIS Pro by sending a close command to the main window.
    /// </summary>
    public void CloseApplication()
    {
        try
        {
            WinAppDriver.CloseApp();
        }
        catch
        {
            ApplicationUtils.KillArcGISProProcess();
        }
    }
}
