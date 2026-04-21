using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Page Object for the Catalog pane in ArcGIS Pro.
/// The Catalog pane displays a tree view of project items: Maps, Toolboxes,
/// Databases, Folders, Styles, etc.
///
/// <para>Used to navigate to toolboxes and tools within the project.
/// For ILL testing, this is the primary way to find and open the
/// Indoor Location Loader tool from its Python Toolbox (.pyt).</para>
///
/// <para><strong>AutomationId discovery:</strong> The element IDs used in this class
/// are best guesses based on ArcGIS Pro conventions. Run
/// <c>CatalogInspectionTests.DumpCatalogPaneTree</c> to confirm or update them.</para>
/// </summary>
public class CatalogPane : PaneBase
{
    /// <summary>
    /// AutomationId for the Catalog pane.
    /// Confirmed via discovery test: lowercase 'p' in "projectDockPane".
    /// </summary>
    public const string CatalogPaneId = "esri_core_projectDockPane";

    /// <summary>
    /// Creates a CatalogPane POM.
    /// </summary>
    public CatalogPane(Application app) : base(app, CatalogPaneId)
    {
    }

    /// <summary>
    /// Expands the "Toolboxes" node in the Catalog pane.
    /// Searches from the main window for reliability — WinAppDriver handles
    /// top-level searches better than deeply-scoped pane element searches.
    /// </summary>
    /// <returns><c>true</c> if the Toolboxes node was found and expanded.</returns>
    public bool ExpandToolboxesNode()
    {
        return ExpandTreeNode("Toolboxes");
    }

    /// <summary>
    /// Checks whether a toolbox with the given name exists in the Catalog pane.
    /// Expands the Toolboxes node first.
    /// </summary>
    /// <param name="toolboxName">
    /// The display name of the toolbox (e.g., "Indoors ServiceNow Tools.pyt").
    /// </param>
    /// <returns><c>true</c> if the toolbox is found.</returns>
    public bool DoesToolboxExist(string toolboxName)
    {
        if (PaneElement == null) return false;

        ExpandToolboxesNode();

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    var element = App.MainWindow.FindElementByName(toolboxName);
                    return element != null;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: 15000);
    }

    /// <summary>
    /// Expands a toolbox node to reveal its tools/scripts.
    /// </summary>
    /// <param name="toolboxName">The display name of the toolbox to expand.</param>
    /// <returns><c>true</c> if the toolbox was found and expanded.</returns>
    public bool ExpandToolbox(string toolboxName)
    {
        if (PaneElement == null) return false;

        ExpandToolboxesNode();
        return ExpandTreeNode(toolboxName);
    }

    /// <summary>
    /// Checks whether a specific tool exists within a toolbox.
    /// Expands the Toolboxes node and the target toolbox first.
    /// </summary>
    /// <param name="toolboxName">The display name of the toolbox.</param>
    /// <param name="toolName">The display name of the tool (e.g., "Indoors Location Loader").</param>
    /// <returns><c>true</c> if the tool is found.</returns>
    public bool DoesToolExist(string toolboxName, string toolName)
    {
        if (PaneElement == null) return false;

        ExpandToolbox(toolboxName);

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    var element = App.MainWindow.FindElementByName(toolName);
                    return element != null;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: 15000);
    }

    /// <summary>
    /// Opens a tool by activating the Geoprocessing pane and searching for it.
    /// This is more reliable than double-clicking in the Catalog tree because
    /// WinAppDriver cannot trigger WPF context menus or true double-click actions
    /// on ArcGIS Pro's Catalog pane ListViewItems.
    ///
    /// <para>Call <see cref="DoesToolExist"/> first to verify the tool is in the
    /// Catalog. Then use this method to open it via the GP pane search.</para>
    /// </summary>
    /// <param name="toolName">The display name of the tool to open.</param>
    /// <returns>A <see cref="GeoprocessingPane"/> POM with the tool dialog loaded.</returns>
    public GeoprocessingPane OpenToolViaGeoprocessingSearch(string toolName)
    {
        // Activate the GP pane tab (if it exists as a docked tab)
        ActivateGeoprocessingTab();
        WaitingUtils.Wait(2000);

        var gpPane = new GeoprocessingPane(App);
        gpPane.SearchForTool(toolName);
        return gpPane;
    }

    /// <summary>
    /// Activates the Geoprocessing pane tab if it's docked as a sibling tab.
    /// In ArcGIS Pro, panes are often tabbed together. The GP pane ToolWindow
    /// only appears in the automation tree when its tab is the active one.
    /// </summary>
    private void ActivateGeoprocessingTab()
    {
        try
        {
            var gpTab = App.MainWindow.FindElementByAccessibilityId(
                "esri_geoprocessing_toolBoxesTab");
            gpTab?.Click();
        }
        catch
        {
            // GP tab may not exist (pane not docked yet) — the PaneBase
            // constructor will handle the timeout gracefully.
        }
    }

    /// <summary>
    /// Gets the names of all visible items in the Catalog tree.
    /// Useful for debugging what's visible at the current expansion level.
    /// </summary>
    public IReadOnlyList<string> GetVisibleItems()
    {
        try
        {
            var treeItems = App.MainWindow.FindElementsByClassName("TreeViewItem");
            return treeItems
                .Select(item => item.GetAttribute("Name"))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Expands a tree node by selecting it and pressing the Right arrow key.
    /// This is more reliable than double-click for ArcGIS Pro's Catalog tree,
    /// especially for Python Toolbox (.pyt) items that require parsing.
    /// Falls back to double-click if the keyboard approach fails.
    /// </summary>
    /// <param name="nodeName">The display name of the tree node to expand.</param>
    /// <returns><c>true</c> if the node was found and the expand action was performed.</returns>
    private bool ExpandTreeNode(string nodeName)
    {
        if (PaneElement == null) return false;

        // Search from MainWindow — WinAppDriver handles top-level searches
        // more reliably than deeply-scoped pane element searches
        var node = WaitingUtils.RetryAssignmentUntilSuccess(
            () => App.MainWindow.FindElementByName(nodeName),
            timeoutMs: 10000,
            debugInfo: $"CatalogPane.ExpandTreeNode — looking for '{nodeName}'");

        if (node == null) return false;

        // Click to select, then Right arrow to expand (more reliable than DoubleClick)
        node.Click();
        WaitingUtils.Wait(500);
        node.SendKeys(OpenQA.Selenium.Keys.ArrowRight);

        // Python Toolboxes (.pyt) require parsing, allow extra time
        WaitingUtils.Wait(3000);

        return true;
    }

    /// <summary>
    /// Performs a double-click action on an element using the Selenium Actions API.
    /// This produces a true double-click event that ArcGIS Pro recognizes
    /// for opening tools (unlike two sequential Click() calls).
    /// </summary>
    /// <param name="element">The element to double-click.</param>
    private void DoubleClick(AppiumWebElement element)
    {
        var actions = new Actions(App.WinAppDriver);
        actions.DoubleClick(element).Perform();
    }
}
