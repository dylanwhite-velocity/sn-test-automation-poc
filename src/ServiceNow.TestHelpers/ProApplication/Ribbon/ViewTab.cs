using ServiceNow.TestHelpers.ProApplication.Pane;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Ribbon;

/// <summary>
/// Page Object for the View ribbon tab in ArcGIS Pro.
/// Provides access to dockable panes: Catalog, Contents, Python, etc.
///
/// <para>The View tab is the primary entry point for opening the Catalog pane,
/// which is needed for navigating toolboxes and folder connections.</para>
/// </summary>
public class ViewTab : RibbonTabBase
{
    /// <summary>
    /// AutomationId for the Catalog Pane button on the View tab.
    /// Run <c>CatalogInspectionTests.DumpRibbonTree</c> to confirm this value.
    /// </summary>
    public const string CatalogPaneButtonId = "esri_core_showProjectDockPane";

    /// <summary>
    /// Creates a ViewTab POM. The View tab AutomationId contains "iewTab".
    /// </summary>
    public ViewTab(Application app) : base(app, "iewTab")
    {
    }

    /// <summary>
    /// Clicks the Catalog Pane button to open the Catalog dockable pane.
    /// If the pane is already open, this will toggle it (close and reopen).
    /// </summary>
    /// <returns>A <see cref="CatalogPane"/> POM for the opened pane.</returns>
    public CatalogPane OpenCatalogPane()
    {
        EnableTab();

        var button = WaitingUtils.RetryAssignmentUntilSuccess(
            () => App.MainWindow.FindElementByAccessibilityId(CatalogPaneButtonId),
            timeoutMs: 10000,
            debugInfo: "ViewTab.OpenCatalogPane — looking for Catalog Pane button");

        if (button == null)
        {
            // Fallback: search by Name in case AutomationId differs
            button = WaitingUtils.RetryAssignmentUntilSuccess(
                () => App.MainWindow.FindElementByName("Catalog Pane"),
                timeoutMs: 5000,
                debugInfo: "ViewTab.OpenCatalogPane — fallback search by Name 'Catalog Pane'");
        }

        if (button == null)
            throw new InvalidOperationException(
                $"Could not find Catalog Pane button (tried AutomationId: {CatalogPaneButtonId} and Name: 'Catalog Pane'). " +
                "Run CatalogInspectionTests.DumpRibbonTree to discover the correct identifier.");

        button.Click();

        // Allow time for the pane to open
        WaitingUtils.Wait(2000);

        return new CatalogPane(App);
    }
}
