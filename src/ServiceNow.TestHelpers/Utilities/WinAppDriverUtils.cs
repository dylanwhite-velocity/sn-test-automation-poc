using System.Diagnostics;

namespace ServiceNow.TestHelpers.Utilities;

/// <summary>
/// Utility methods for managing the WinAppDriver process lifecycle.
/// Mirrors the CUIT <c>WinAppDriverUtilities</c> pattern.
/// WinAppDriver is started/stopped programmatically in <c>[AssemblyInitialize]</c>/<c>[AssemblyCleanup]</c>.
/// </summary>
public static class WinAppDriverUtils
{
    /// <summary>
    /// Default install paths for WinAppDriver (checked in order).
    /// </summary>
    private static readonly string[] WadPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Windows Application Driver", "WinAppDriver.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Windows Application Driver", "WinAppDriver.exe")
    };

    /// <summary>
    /// Starts a new WinAppDriver process, killing any existing ones first.
    /// </summary>
    /// <returns><c>true</c> if WinAppDriver started successfully.</returns>
    public static bool StartWinAppDriver()
    {
        CloseWinAppDriver();

        var wadPath = WadPaths.FirstOrDefault(File.Exists);
        if (wadPath == null)
        {
            Trace.WriteLine("*** WinAppDriver is not installed. Install from: https://github.com/microsoft/WinAppDriver/releases ***");
            return false;
        }

        var initialCount = GetProcessCount();

        try
        {
            Process.Start(wadPath);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"*** Failed to start WinAppDriver: {ex.Message} ***");
            return false;
        }

        var started = WaitingUtils.RetryUntilSuccessOrTimeout(
            () => GetProcessCount() > initialCount,
            timeoutMs: 10000);

        if (!started)
            Trace.WriteLine("*** WinAppDriver did not start within 10 seconds ***");

        return started;
    }

    /// <summary>
    /// Closes all running WinAppDriver processes.
    /// </summary>
    public static void CloseWinAppDriver()
    {
        foreach (var process in Process.GetProcessesByName("WinAppDriver"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(3000);
            }
            catch
            {
                Trace.WriteLine($"*** Could not kill WinAppDriver process (PID: {process.Id}) ***");
            }
        }
    }

    /// <summary>
    /// Gets the number of running WinAppDriver processes.
    /// </summary>
    public static int GetProcessCount()
    {
        return Process.GetProcessesByName("WinAppDriver").Length;
    }
}
