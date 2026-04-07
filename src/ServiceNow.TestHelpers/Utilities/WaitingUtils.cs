using System.Diagnostics;

namespace ServiceNow.TestHelpers.Utilities;

/// <summary>
/// Retry/wait utility methods. Mirrors the CUIT <c>WaitingUtilities</c> pattern.
/// Provides robust retry-until-success loops used throughout the POM classes
/// instead of <c>Thread.Sleep()</c>.
/// </summary>
public static class WaitingUtils
{
    /// <summary>Default timeout in milliseconds.</summary>
    public const int DefaultTimeoutMs = 10000;

    /// <summary>Default delay between retry attempts in milliseconds.</summary>
    public const int DefaultDelayMs = 500;

    /// <summary>
    /// Retries a condition function until it returns <c>true</c> or the timeout expires.
    /// </summary>
    /// <param name="condition">A function that returns <c>true</c> when the condition is met.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <param name="delayBetweenAttemptsMs">Delay between each retry in milliseconds.</param>
    /// <returns><c>true</c> if the condition was met within the timeout.</returns>
    public static bool RetryUntilSuccessOrTimeout(
        Func<bool> condition,
        int timeoutMs = DefaultTimeoutMs,
        int delayBetweenAttemptsMs = DefaultDelayMs)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition()) return true;
            }
            catch
            {
                // Swallow exceptions during retry
            }

            Thread.Sleep(delayBetweenAttemptsMs);
        }

        return false;
    }

    /// <summary>
    /// Retries an assignment function until it returns a non-null value or the timeout expires.
    /// Mirrors the CUIT <c>RetryAssignmentUntilSuccessOrTimeout</c> pattern.
    /// </summary>
    /// <typeparam name="T">The type of value to assign.</typeparam>
    /// <param name="assignmentFunc">A function that returns the value (or null if not yet available).</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <param name="delayBetweenAttemptsMs">Delay between each retry in milliseconds.</param>
    /// <param name="debugInfo">Optional debug info for trace logging.</param>
    /// <returns>The assigned value, or <c>null</c> if the timeout expired.</returns>
    public static T? RetryAssignmentUntilSuccess<T>(
        Func<T?> assignmentFunc,
        int timeoutMs = DefaultTimeoutMs,
        int delayBetweenAttemptsMs = DefaultDelayMs,
        string? debugInfo = null) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var result = assignmentFunc();
                if (result != null) return result;
            }
            catch
            {
                // Swallow exceptions during retry
            }

            Thread.Sleep(delayBetweenAttemptsMs);
        }

        if (debugInfo != null)
        {
            Trace.WriteLine($"[WaitingUtils] Timeout ({timeoutMs}ms) waiting for: {debugInfo}");
        }

        return null;
    }

    /// <summary>
    /// Waits for a fixed duration. Use sparingly — prefer <see cref="RetryUntilSuccessOrTimeout"/>
    /// for UI interactions. This is acceptable for initial application startup waits.
    /// </summary>
    /// <param name="milliseconds">Duration to wait.</param>
    public static void Wait(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }
}
