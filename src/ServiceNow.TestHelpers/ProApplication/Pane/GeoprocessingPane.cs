using OpenQA.Selenium.Appium;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Page Object for the Geoprocessing pane in ArcGIS Pro.
/// Used for ILL (Indoor Location Loader) tool execution testing.
/// Mirrors the CUIT <c>Geoprocessing</c> pane class.
///
/// <para>After searching for and opening a tool, use <see cref="GetToolDialog"/>
/// to get a <see cref="GpToolDialog"/> for parameter interaction.</para>
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
    /// The currently loaded tool dialog, or <c>null</c> if no tool is open.
    /// Populated by <see cref="GetToolDialog"/>.
    /// </summary>
    public GpToolDialog? ToolDialogPage { get; private set; }

    /// <summary>
    /// Gets or creates a <see cref="GpToolDialog"/> for the currently loaded GP tool.
    /// The tool must already be loaded in the pane (via <see cref="SearchForTool"/>).
    /// </summary>
    /// <returns>A <see cref="GpToolDialog"/> wrapping the tool dialog.</returns>
    /// <exception cref="InvalidOperationException">If no tool dialog is loaded.</exception>
    public GpToolDialog GetToolDialog()
    {
        if (PaneElement == null)
            throw new InvalidOperationException("Geoprocessing pane is not open.");

        var toolDialogElement = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                try
                {
                    return PaneElement.FindElementByAccessibilityId(GpToolDialog.ToolDialogAutomationId);
                }
                catch
                {
                    // Fallback: search from MainWindow
                    return App.MainWindow.FindElementByAccessibilityId(GpToolDialog.ToolDialogAutomationId);
                }
            },
            timeoutMs: 15000,
            debugInfo: "GeoprocessingPane.GetToolDialog — looking for gp_tool_dialog");

        if (toolDialogElement == null)
            throw new InvalidOperationException(
                "GP tool dialog not found. Ensure a tool has been opened with SearchForTool().");

        ToolDialogPage = new GpToolDialog(App, toolDialogElement);
        return ToolDialogPage;
    }

    /// <summary>
    /// Searches for a geoprocessing tool by name in the search box.
    /// Verifies the tool loaded after search and retries once if the keyboard
    /// navigation didn't open the tool on the first attempt.
    /// </summary>
    /// <param name="toolName">The name of the tool to search for (e.g., "Indoor Location Loader").</param>
    public void SearchForTool(string toolName)
    {
        if (PaneElement == null)
            throw new InvalidOperationException("Geoprocessing pane is not open.");

        // Attempt search up to 2 times — keyboard navigation can miss on first try
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            PerformToolSearch(toolName);

            // Verify the tool actually opened (Run button present)
            if (DidToolLoad()) return;

            if (attempt < 2)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[GeoprocessingPane] Tool '{toolName}' did not load on attempt {attempt}, retrying...");
            }
        }
    }

    /// <summary>
    /// Performs a single tool search attempt: types the tool name, submits,
    /// and uses keyboard navigation to open the first result.
    /// </summary>
    /// <param name="toolName">The name of the tool to search for.</param>
    private void PerformToolSearch(string toolName)
    {
        // The GP pane search box has AutomationId "search_ctrl" (discovered via dump).
        // The Catalog pane has a separate "searchTextBox" — they use different IDs.
        var searchBox = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                try
                {
                    return PaneElement!.FindElementByAccessibilityId("search_ctrl");
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
