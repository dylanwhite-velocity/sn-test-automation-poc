using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;
using ServiceNow.TestHelpers.ProApplication;
using System.Diagnostics;

namespace ServiceNow.TestHelpers.Utilities;

/// <summary>
/// Utility methods for managing the ArcGIS Pro application lifecycle.
/// Mirrors the CUIT <c>ApplicationUtils</c> pattern: start/stop Pro via WinAppDriver,
/// manage desktop sessions, and interact with Pro processes.
/// </summary>
public static class ApplicationUtils
{
    /// <summary>Default WinAppDriver endpoint URL.</summary>
    public const string DefaultWinAppDriverUrl = "http://127.0.0.1:4723";

    /// <summary>Default implicit wait timeout in milliseconds.</summary>
    public const int ImplicitWaitTimeoutMs = 20000;

    /// <summary>Maximum number of retry attempts for WinAppDriver session creation.</summary>
    private const int MaxSessionRetries = 3;

    /// <summary>Delay in milliseconds between session creation retries.</summary>
    private const int SessionRetryDelayMs = 5000;

    /// <summary>
    /// Starts ArcGIS Pro via WinAppDriver and returns a <see cref="WindowsDriver{AppiumWebElement}"/> session.
    /// Retries session creation up to <see cref="MaxSessionRetries"/> times to handle
    /// transient WinAppDriver startup timing issues.
    /// </summary>
    /// <param name="proExePath">Full path to ArcGISPro.exe.</param>
    /// <param name="winAppDriverUrl">WinAppDriver endpoint URL.</param>
    /// <param name="commandLineArgs">Optional command-line arguments for Pro (e.g., a .aprx path).</param>
    /// <returns>A WinAppDriver session targeting ArcGIS Pro.</returns>
    public static WindowsDriver<AppiumWebElement> StartApplicationWAD(
        string proExePath,
        string winAppDriverUrl = DefaultWinAppDriverUrl,
        string? commandLineArgs = null)
    {
        var appCapabilities = new AppiumOptions();
        appCapabilities.AddAdditionalCapability("app", proExePath);
        appCapabilities.AddAdditionalCapability("deviceName", "WindowsPC");
        appCapabilities.AddAdditionalCapability("ms:waitForAppLaunch", "30");

        if (!string.IsNullOrEmpty(commandLineArgs))
        {
            appCapabilities.AddAdditionalCapability("appArguments", commandLineArgs);
        }

        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxSessionRetries; attempt++)
        {
            try
            {
                var driver = new WindowsDriver<AppiumWebElement>(
                    new Uri(winAppDriverUrl),
                    appCapabilities,
                    TimeSpan.FromSeconds(120));

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(ImplicitWaitTimeoutMs);
                return driver;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Trace.WriteLine($"[ApplicationUtils] Session creation attempt {attempt}/{MaxSessionRetries} failed: {ex.Message}");

                if (attempt < MaxSessionRetries)
                {
                    Thread.Sleep(SessionRetryDelayMs);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to create WinAppDriver session after {MaxSessionRetries} attempts.",
            lastException);
    }

    /// <summary>
    /// Gets a root desktop session from WinAppDriver. Useful for finding windows
    /// that exist outside the main Pro window.
    /// </summary>
    /// <param name="winAppDriverUrl">WinAppDriver endpoint URL.</param>
    /// <param name="timeoutSeconds">Implicit wait timeout for the session.</param>
    /// <returns>A root desktop WinAppDriver session.</returns>
    public static WindowsDriver<AppiumWebElement> GetRootSession(
        string winAppDriverUrl = DefaultWinAppDriverUrl,
        int timeoutSeconds = 10)
    {
        var desktopCapabilities = new AppiumOptions();
        desktopCapabilities.AddAdditionalCapability("app", "Root");
        desktopCapabilities.AddAdditionalCapability("deviceName", "WindowsPC");

        return new WindowsDriver<AppiumWebElement>(
            new Uri(winAppDriverUrl),
            desktopCapabilities,
            TimeSpan.FromSeconds(timeoutSeconds));
    }

    /// <summary>
    /// Gets an existing desktop session and attaches to a running ArcGIS Pro instance.
    /// The intermediate root session used to locate the Pro window is disposed before returning.
    /// </summary>
    /// <param name="winAppDriverUrl">WinAppDriver endpoint URL.</param>
    /// <returns>A WinAppDriver session attached to the Pro main window.</returns>
    public static WindowsDriver<AppiumWebElement> GetExistingDesktopSession(
        string winAppDriverUrl = DefaultWinAppDriverUrl)
    {
        var rootSession = GetRootSession(winAppDriverUrl);

        try
        {
            var proWindow = WaitingUtils.RetryAssignmentUntilSuccess(
                () => rootSession.FindElementByAccessibilityId(ActiProBase.MainWindowAutomationId),
                timeoutMs: 30000,
                debugInfo: "GetExistingDesktopSession — looking for Pro main window");

            if (proWindow == null)
                throw new InvalidOperationException("ArcGIS Pro main window not found.");

            var proWindowHandle = proWindow.GetAttribute("NativeWindowHandle");
            var hexHandle = int.Parse(proWindowHandle).ToString("x");

            var appCapabilities = new AppiumOptions();
            appCapabilities.AddAdditionalCapability("appTopLevelWindow", hexHandle);
            appCapabilities.AddAdditionalCapability("deviceName", "WindowsPC");

            return new WindowsDriver<AppiumWebElement>(
                new Uri(winAppDriverUrl),
                appCapabilities);
        }
        finally
        {
            // Dispose the root session to prevent session leaks
            try { rootSession.Quit(); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Kills all running ArcGIS Pro processes.
    /// </summary>
    public static void KillArcGISProProcess()
    {
        foreach (var process in Process.GetProcessesByName("ArcGISPro"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(5000);
            }
            catch
            {
                // Swallow — best effort
            }
        }
    }

    /// <summary>
    /// Gets the count of running ArcGIS Pro processes.
    /// </summary>
    public static int GetArcGISProCount()
    {
        return Process.GetProcessesByName("ArcGISPro").Length;
    }
}
