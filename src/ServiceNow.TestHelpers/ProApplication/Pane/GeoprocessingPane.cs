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
    /// <summary>
    /// AutomationId for the Geoprocessing pane ToolWindow.
    /// Discovered via CatalogInspectionTests: the actual ID is "esri_geoprocessing_toolBoxes",
    /// not the commonly assumed "esri_geoprocessing_GeoprocessingDockPane".
    /// </summary>
    public const string GeoprocessingPaneId = "esri_geoprocessing_toolBoxes";

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

        // The GP pane search box has AutomationId "search_ctrl" (discovered via dump).
        // The Catalog pane has a separate "searchTextBox" — they use different IDs.
        var searchBox = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                try
                {
                    return PaneElement.FindElementByAccessibilityId("search_ctrl");
                }
                catch
                {
                    return App.MainWindow.FindElementByAccessibilityId("search_ctrl");
                }
            },
            timeoutMs: 10000,
            debugInfo: "GeoprocessingPane.SearchForTool — looking for search_ctrl");

        searchBox?.Clear();
        searchBox?.SendKeys(toolName);
        searchBox?.SendKeys(OpenQA.Selenium.Keys.Return);

        // Wait for search results to load
        WaitingUtils.Wait(5000);
        WaitingUtils.RetryUntilSuccessOrTimeout(
            () => !IsProgressBarVisible(),
            timeoutMs: 15000);

        // Navigate from search box to the first result using keyboard,
        // then open it. This avoids name-based element lookup ambiguity.
        WaitingUtils.Wait(1000);
        searchBox?.SendKeys(OpenQA.Selenium.Keys.Tab);
        WaitingUtils.Wait(500);
        searchBox?.SendKeys(OpenQA.Selenium.Keys.ArrowDown);
        WaitingUtils.Wait(500);

        // Press Enter on the focused search result to open the tool dialog
        var enterAction = new OpenQA.Selenium.Interactions.Actions(App.WinAppDriver);
        enterAction.SendKeys(OpenQA.Selenium.Keys.Return).Perform();
        WaitingUtils.Wait(5000);
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
    /// Checks for a "Run" button which is present on all GP tool dialogs.
    /// </summary>
    public bool DidToolLoad()
    {
        if (PaneElement == null) return false;

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    // Try scoped search first, then MainWindow fallback
                    try
                    {
                        var runButton = PaneElement.FindElementsByName("Run");
                        if (runButton.Any()) return true;
                    }
                    catch { }

                    // Fallback: search from MainWindow for "Run" button
                    var runFromMain = App.MainWindow.FindElementByName("Run");
                    return runFromMain != null;
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
