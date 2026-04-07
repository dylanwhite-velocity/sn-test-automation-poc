using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ServiceNow.TestHelpers.Utilities;

/// <summary>
/// Screenshot capture utilities for test failure documentation.
/// Mirrors the CUIT <c>ScreenCaptureUtilities</c> pattern.
/// </summary>
public static class ScreenCaptureUtils
{
    /// <summary>
    /// Captures the primary screen and saves it to the specified file path.
    /// Only works on Windows where System.Drawing is available.
    /// </summary>
    /// <param name="filePath">Full path where the screenshot PNG will be saved.</param>
    public static void CapturePrimaryScreen(string filePath)
    {
        try
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Use System.Drawing to capture screen (Windows only)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CaptureScreenWindows(filePath);
            }
            else
            {
                Trace.WriteLine("[ScreenCaptureUtils] Screen capture is only supported on Windows.");
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[ScreenCaptureUtils] Failed to capture screen: {ex.Message}");
        }
    }

    /// <summary>
    /// Windows-specific screen capture using System.Drawing and Win32 APIs.
    /// </summary>
    private static void CaptureScreenWindows(string filePath)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        var screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
        var screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

        if (screenWidth == 0 || screenHeight == 0)
        {
            Trace.WriteLine("[ScreenCaptureUtils] Could not determine screen dimensions.");
            return;
        }

        using var bitmap = new Bitmap(screenWidth, screenHeight);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        bitmap.Save(filePath, ImageFormat.Png);
#pragma warning restore CA1416
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
