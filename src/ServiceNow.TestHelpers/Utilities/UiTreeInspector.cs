using OpenQA.Selenium.Appium;
using System.Diagnostics;
using System.Text;

namespace ServiceNow.TestHelpers.Utilities;

/// <summary>
/// Programmatically walks the WinAppDriver UI element tree and dumps element
/// properties to a structured text file. Replaces manual use of Inspect.exe
/// for discovering AutomationIds, Names, and ClassNames of ArcGIS Pro UI elements.
///
/// <para><strong>Usage:</strong> Call <see cref="DumpElementTree"/> from a test method
/// to produce a readable dump of the UI tree starting from any root element.
/// Output files are saved to the TestResults directory.</para>
///
/// <para><strong>Why this exists:</strong> ArcGIS Pro's WPF element tree is deep and
/// complex. Inspect.exe requires a manual point-and-click workflow. This utility
/// automates that by walking the tree recursively and writing all element metadata
/// to a file that can be searched with grep or a text editor.</para>
/// </summary>
public static class UiTreeInspector
{
    /// <summary>
    /// Recursively dumps the UI element tree starting from <paramref name="root"/>
    /// to a plain-text file at <paramref name="outputPath"/>.
    ///
    /// <para>WinAppDriver does not support recursive XPath child traversal reliably.
    /// This method uses <c>FindElementsByXPath("//*")</c> to get a flat list of ALL
    /// descendant elements, then dumps each one's properties. The <paramref name="maxDepth"/>
    /// parameter is unused but retained for API compatibility.</para>
    /// </summary>
    /// <param name="root">The root element to start the dump from.</param>
    /// <param name="outputPath">Full path to the output text file.</param>
    /// <param name="maxDepth">Unused (retained for API compatibility).</param>
    /// <returns>The number of elements dumped.</returns>
    public static int DumpElementTree(AppiumWebElement root, string outputPath, int maxDepth = 5)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== UI Element Tree Dump (Flat) ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('=', 100));
        sb.AppendLine();
        sb.AppendLine("Format: ClassName | AutomationId | Name | ControlType | IsOffscreen");
        sb.AppendLine(new string('-', 100));

        // Dump the root element first
        sb.AppendLine($"[ROOT] {InspectElement(root)}");
        var count = 1;

        try
        {
            // WinAppDriver: "//*" returns all descendants in a flat list
            var allDescendants = root.FindElementsByXPath("//*");
            foreach (var element in allDescendants)
            {
                try
                {
                    sb.AppendLine(InspectElement(element));
                    count++;
                }
                catch
                {
                    sb.AppendLine("[ERROR] Could not inspect element");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[ERROR] Failed to enumerate descendants: {ex.Message}");
        }

        sb.AppendLine();
        sb.AppendLine(new string('=', 100));
        sb.AppendLine($"Total elements dumped: {count}");

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString());
        Trace.WriteLine($"[UiTreeInspector] Dumped {count} elements to: {outputPath}");

        return count;
    }

    /// <summary>
    /// Dumps a single element and its properties as a formatted string (no recursion).
    /// Useful for quick inline inspection during test development.
    /// </summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>A formatted string with the element's key properties.</returns>
    public static string InspectElement(AppiumWebElement element)
    {
        var automationId = SafeGetAttribute(element, "AutomationId");
        var name = SafeGetAttribute(element, "Name");
        var className = SafeGetAttribute(element, "ClassName");
        var controlType = SafeGetAttribute(element, "LocalizedControlType");
        var isOffscreen = SafeGetAttribute(element, "IsOffscreen");

        return $"ClassName: {className} | AutomationId: {automationId} | Name: {name} | ControlType: {controlType} | IsOffscreen: {isOffscreen}";
    }

    /// <summary>
    /// Searches all descendant elements for those matching a predicate.
    /// Uses <c>FindElementsByXPath("//*")</c> for a flat descendant search.
    /// </summary>
    /// <param name="root">The root element to search from.</param>
    /// <param name="predicate">A function that receives (automationId, name, className) and returns true for matches.</param>
    /// <param name="maxDepth">Unused (retained for API compatibility).</param>
    /// <returns>List of matching element descriptions.</returns>
    public static IReadOnlyList<string> FindElements(
        AppiumWebElement root,
        Func<string, string, string, bool> predicate,
        int maxDepth = 5)
    {
        var results = new List<string>();

        try
        {
            var allDescendants = root.FindElementsByXPath("//*");
            foreach (var element in allDescendants)
            {
                try
                {
                    var automationId = SafeGetAttribute(element, "AutomationId");
                    var name = SafeGetAttribute(element, "Name");
                    var className = SafeGetAttribute(element, "ClassName");

                    if (predicate(automationId, name, className))
                    {
                        results.Add($"{InspectElement(element)}");
                    }
                }
                catch
                {
                    // Stale element — skip
                }
            }
        }
        catch
        {
            // Failed to enumerate
        }

        return results.AsReadOnly();
    }

    private static string SafeGetAttribute(AppiumWebElement element, string attributeName)
    {
        try
        {
            return element.GetAttribute(attributeName) ?? "";
        }
        catch
        {
            return "<error>";
        }
    }
}
