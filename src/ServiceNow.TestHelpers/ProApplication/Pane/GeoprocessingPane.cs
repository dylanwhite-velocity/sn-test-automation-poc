using OpenQA.Selenium.Appium;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Page Object for the Geoprocessing pane in ArcGIS Pro.
/// Used for ILL (Indoor Location Loader) tool execution testing.
/// Mirrors the CUIT <c>Geoprocessing</c> pane class.
/// </summary>
public class GeoprocessingPane : PaneBase
{
    /// <summary>AutomationId for the Geoprocessing pane.</summary>
    public const string GeoprocessingPaneId = "esri_geoprocessing_GeoprocessingDockPane";

    /// <summary>
    /// Creates a GeoprocessingPane POM.
    /// </summary>
    public GeoprocessingPane(Application app) : base(app, GeoprocessingPaneId)
    {
    }

    /// <summary>
    /// Searches for a geoprocessing tool by name in the search box.
    /// </summary>
    /// <param name="toolName">The name of the tool to search for (e.g., "Indoor Location Loader").</param>
    public void SearchForTool(string toolName)
    {
        if (PaneElement == null)
            throw new InvalidOperationException("Geoprocessing pane is not open.");

        var searchBox = WaitingUtils.RetryAssignmentUntilSuccess(
            () => PaneElement.FindElementByAccessibilityId("SearchTextBox"),
            timeoutMs: 10000,
            debugInfo: "GeoprocessingPane.SearchForTool — looking for SearchTextBox");

        searchBox?.Clear();
        searchBox?.SendKeys(toolName);
        searchBox?.SendKeys(OpenQA.Selenium.Keys.Return);

        // Wait for search results
        WaitingUtils.RetryUntilSuccessOrTimeout(
            () => !IsProgressBarVisible(),
            timeoutMs: 15000);
    }

    /// <summary>
    /// Returns <c>true</c> if the GP progress bar is currently visible (tool loading/running).
    /// </summary>
    public bool IsProgressBarVisible()
    {
        if (PaneElement == null) return false;

        try
        {
            var progressBar = PaneElement.FindElementsByClassName("ProgressBar");
            return progressBar.Any(pb => !pb.GetAttribute("IsOffscreen").Equals("True", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if a tool dialog has loaded in the Geoprocessing pane.
    /// </summary>
    public bool DidToolLoad()
    {
        if (PaneElement == null) return false;

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    // Look for the Run button as indicator that a tool dialog loaded
                    var runButton = PaneElement.FindElementsByName("Run");
                    return runButton.Any();
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: 15000);
    }

    /// <summary>
    /// Clicks the Run button to execute the currently open geoprocessing tool.
    /// </summary>
    public void ClickRun()
    {
        if (PaneElement == null)
            throw new InvalidOperationException("Geoprocessing pane is not open.");

        var runButton = WaitingUtils.RetryAssignmentUntilSuccess(
            () => PaneElement.FindElementByName("Run"),
            timeoutMs: 10000,
            debugInfo: "GeoprocessingPane.ClickRun — looking for Run button");

        runButton?.Click();
    }
}
