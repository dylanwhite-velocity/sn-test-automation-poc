using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.Integration.Tests.Discovery;

/// <summary>
/// Discovery tests that dump the ArcGIS Pro UI element tree to text files.
/// These replace the manual Inspect.exe workflow for finding AutomationIds,
/// Names, and ClassNames of UI elements.
///
/// <para><strong>When to run:</strong>
/// <list type="bullet">
///   <item>First time setting up a new POM class — to discover element identifiers.</item>
///   <item>After ArcGIS Pro updates — to detect element ID changes.</item>
///   <item>When a POM class can't find an element — to debug the UI tree.</item>
/// </list>
/// </para>
///
/// <para>Output files are saved to the TestResults directory and are gitignored.
/// Run with: <c>dotnet test --filter "FullyQualifiedName~CatalogInspectionTests"</c></para>
/// </summary>
[TestClass]
public class CatalogInspectionTests : ServiceNowTestBase
{
    /// <summary>MSTest context for logging and output paths.</summary>
    public new TestContext? TestContext { get; set; }

    /// <summary>
    /// Path to the ArcGIS Pro project used for inspection.
    /// This should be a project that has toolboxes and folder connections configured.
    /// </summary>
    private const string InspectionProjectPath =
        @"C:\Users\dyl13740\Documents\ArcGIS\Projects\MyProject\MyProject.aprx";

    /// <summary>
    /// Dumps the top-level UI element tree of the ArcGIS Pro main window.
    /// Produces a broad overview (depth 3) useful for finding major panes,
    /// ribbon tabs, and top-level containers.
    ///
    /// <para>Output: <c>TestResults/MainWindowTree.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Dumps the ArcGIS Pro main window element tree for AutomationId discovery")]
    public void DumpMainWindowTree()
    {
        // Arrange — launch Pro with the inspection project
        var app = StartProWithProject(InspectionProjectPath);
        Assert.IsNotNull(app, "Application POM should be created");
        Assert.IsNotNull(app.MainWindow, "Main window should be found");

        // Act — dump the main window tree at depth 3 (broad overview)
        var outputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "MainWindowTree.txt");

        var elementCount = UiTreeInspector.DumpElementTree(
            app.MainWindow, outputPath, maxDepth: 3);

        // Assert — we got some elements
        TestContext?.WriteLine($"Dumped {elementCount} elements to: {outputPath}");
        TestContext?.AddResultFile(outputPath);
        Assert.IsTrue(elementCount > 0, "Should have found elements in the main window");
    }

    /// <summary>
    /// Opens the Catalog pane and dumps its element subtree at deep depth (6).
    /// This is the primary discovery test for finding Catalog tree element IDs
    /// (Toolboxes node, toolbox items, script tools).
    ///
    /// <para>The Catalog pane is opened by navigating to the View ribbon tab and
    /// clicking the Catalog Pane button. If the pane is already open, we find
    /// it directly.</para>
    ///
    /// <para>Output: <c>TestResults/CatalogPaneTree.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Dumps the Catalog pane element tree for Toolbox/Tool AutomationId discovery")]
    public void DumpCatalogPaneTree()
    {
        // Arrange — launch Pro with the inspection project
        var app = StartProWithProject(InspectionProjectPath);
        Assert.IsNotNull(app, "Application POM should be created");

        // Act — try to find or open the Catalog pane
        // First, try finding it directly (it may already be open)
        var catalogPane = TryFindCatalogPane(app);

        if (catalogPane == null)
        {
            // Open it via View tab → Catalog Pane button
            TestContext?.WriteLine("Catalog pane not found. Attempting to open via View tab...");
            TryOpenCatalogViaViewTab(app);
            catalogPane = TryFindCatalogPane(app);
        }

        Assert.IsNotNull(catalogPane, "Catalog pane should be found after opening. " +
            "Check the AutomationId — run DumpMainWindowTree first to find the correct ID.");

        // Dump the Catalog pane tree at deep depth
        var outputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "CatalogPaneTree.txt");

        var elementCount = UiTreeInspector.DumpElementTree(
            catalogPane, outputPath, maxDepth: 6);

        TestContext?.WriteLine($"Dumped {elementCount} Catalog pane elements to: {outputPath}");
        TestContext?.AddResultFile(outputPath);
        Assert.IsTrue(elementCount > 0, "Should have found elements in the Catalog pane");

        // Also search for elements containing "Toolbox" or "Tool" in their names
        var toolboxElements = UiTreeInspector.FindElements(
            catalogPane,
            (automationId, name, className) =>
                name.Contains("Toolbox", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Tool", StringComparison.OrdinalIgnoreCase) ||
                automationId.Contains("Toolbox", StringComparison.OrdinalIgnoreCase) ||
                automationId.Contains("catalog", StringComparison.OrdinalIgnoreCase),
            maxDepth: 6);

        var searchOutputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "CatalogPaneTree_ToolboxMatches.txt");

        File.WriteAllLines(searchOutputPath, new[] { "=== Elements matching 'Toolbox', 'Tool', or 'catalog' ===" }
            .Concat(toolboxElements.Select(e => e)));

        TestContext?.AddResultFile(searchOutputPath);
        TestContext?.WriteLine($"Found {toolboxElements.Count} elements matching toolbox/tool patterns");
    }

    /// <summary>
    /// Dumps the ribbon area to help discover the View tab and its buttons.
    /// Useful for finding the correct AutomationId for the Catalog Pane button.
    ///
    /// <para>Output: <c>TestResults/RibbonTree.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Dumps the ribbon element tree to find View tab and Catalog Pane button IDs")]
    public void DumpRibbonTree()
    {
        // Arrange
        var app = StartProWithProject(InspectionProjectPath);
        Assert.IsNotNull(app, "Application POM should be created");

        // Find the ribbon element
        var ribbon = WaitingUtils.RetryAssignmentUntilSuccess(
            () => app.MainWindow.FindElementByAccessibilityId("NewRibbon"),
            timeoutMs: 15000,
            debugInfo: "DumpRibbonTree — looking for ribbon");

        Assert.IsNotNull(ribbon, "Ribbon should be found");

        // Dump ribbon tree
        var outputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "RibbonTree.txt");

        var elementCount = UiTreeInspector.DumpElementTree(ribbon, outputPath, maxDepth: 4);

        TestContext?.WriteLine($"Dumped {elementCount} ribbon elements to: {outputPath}");
        TestContext?.AddResultFile(outputPath);

        // Search for View-related elements
        var viewElements = UiTreeInspector.FindElements(
            ribbon,
            (automationId, name, className) =>
                name.Contains("View", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Catalog", StringComparison.OrdinalIgnoreCase) ||
                automationId.Contains("iew", StringComparison.OrdinalIgnoreCase) ||
                automationId.Contains("catalog", StringComparison.OrdinalIgnoreCase),
            maxDepth: 4);

        TestContext?.WriteLine($"View/Catalog related elements found: {viewElements.Count}");
        foreach (var elem in viewElements)
        {
            TestContext?.WriteLine($"  {elem}");
        }
    }

    /// <summary>
    /// Diagnostic test: Navigates the Catalog to the ILL tool (same steps as the
    /// ILL test) and dumps the UI tree after each step. Used to debug why
    /// double-clicking the tool doesn't open the GP pane.
    ///
    /// <para>Output files: <c>TestResults/DiagStep*.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Diagnostic: trace Catalog navigation to ILL tool, dump UI state at each step")]
    public void DiagnoseCatalogToolNavigation()
    {
        var app = StartProWithProject(InspectionProjectPath);
        Assert.IsNotNull(app, "Application POM should be created");

        var resultsDir = TestContext?.TestResultsDirectory ?? ".";

        // Step 1: Open Catalog pane
        TestContext?.WriteLine("Step 1: Opening Catalog pane via View tab...");
        var viewTab = new ServiceNow.TestHelpers.ProApplication.Ribbon.ViewTab(app);
        var catalogPane = viewTab.OpenCatalogPane();
        Assert.IsTrue(catalogPane.IsPaneOpen(), "Catalog pane should be open");

        // Step 2: Expand Toolboxes
        TestContext?.WriteLine("Step 2: Expanding Toolboxes node...");
        bool toolboxesExpanded = catalogPane.ExpandToolboxesNode();
        TestContext?.WriteLine($"  Toolboxes expanded: {toolboxesExpanded}");

        // Dump tree after Toolboxes expansion
        var step2Path = Path.Combine(resultsDir, "DiagStep2_AfterToolboxes.txt");
        var count2 = UiTreeInspector.DumpElementTree(app.MainWindow, step2Path, maxDepth: 3);
        TestContext?.WriteLine($"  Dumped {count2} elements to {step2Path}");
        TestContext?.AddResultFile(step2Path);

        // Step 3: Expand the toolbox
        TestContext?.WriteLine("Step 3: Expanding 'Indoors ServiceNow Tools.pyt'...");
        bool toolboxExpanded = catalogPane.ExpandToolbox("Indoors ServiceNow Tools.pyt");
        TestContext?.WriteLine($"  Toolbox expanded: {toolboxExpanded}");

        // Dump tree after toolbox expansion
        var step3Path = Path.Combine(resultsDir, "DiagStep3_AfterToolbox.txt");
        var count3 = UiTreeInspector.DumpElementTree(app.MainWindow, step3Path, maxDepth: 3);
        TestContext?.WriteLine($"  Dumped {count3} elements to {step3Path}");
        TestContext?.AddResultFile(step3Path);

        // Step 4: Find the tool element and inspect it
        TestContext?.WriteLine("Step 4: Finding 'Indoors Location Loader' element...");
        OpenQA.Selenium.Appium.AppiumWebElement? toolElement = null;
        try
        {
            toolElement = app.MainWindow.FindElementByName("Indoors Location Loader");
        }
        catch
        {
            TestContext?.WriteLine("  FindElementByName failed. Trying FindElementByAccessibilityId...");
            try
            {
                toolElement = app.MainWindow.FindElementByAccessibilityId("Indoors Location Loader");
            }
            catch
            {
                TestContext?.WriteLine("  FindElementByAccessibilityId also failed.");
            }
        }

        if (toolElement != null)
        {
            TestContext?.WriteLine($"  FOUND tool element:");
            TestContext?.WriteLine($"    Name: {toolElement.GetAttribute("Name")}");
            TestContext?.WriteLine($"    AutomationId: {toolElement.GetAttribute("AutomationId")}");
            TestContext?.WriteLine($"    ClassName: {toolElement.GetAttribute("ClassName")}");
            TestContext?.WriteLine($"    IsOffscreen: {toolElement.GetAttribute("IsOffscreen")}");
            TestContext?.WriteLine($"    Location: {toolElement.Location}");
            TestContext?.WriteLine($"    Size: {toolElement.Size}");
            TestContext?.WriteLine($"    Enabled: {toolElement.Enabled}");
            TestContext?.WriteLine($"    Displayed: {toolElement.Displayed}");

            // Step 5: Right-click the tool to see context menu options
            TestContext?.WriteLine("Step 5: Right-clicking tool element to inspect context menu...");
            var actions = new OpenQA.Selenium.Interactions.Actions(app.WinAppDriver);
            actions.ContextClick(toolElement).Perform();
            WaitingUtils.Wait(2000);

            // Dump all elements to capture the context menu
            var step5ContextPath = Path.Combine(resultsDir, "DiagStep5_ContextMenu.txt");
            var countCtx = UiTreeInspector.DumpElementTree(app.MainWindow, step5ContextPath, maxDepth: 3);
            TestContext?.WriteLine($"  Dumped {countCtx} elements (with context menu) to {step5ContextPath}");
            TestContext?.AddResultFile(step5ContextPath);

            // Also search for menu-related elements
            TestContext?.WriteLine("  Searching for menu items...");
            var menuItems = UiTreeInspector.FindElements(
                app.MainWindow,
                (automationId, name, className) =>
                    className.Contains("MenuItem") ||
                    className.Contains("Menu") ||
                    className.Contains("Popup") ||
                    className.Contains("Context") ||
                    name.Equals("Open", StringComparison.OrdinalIgnoreCase),
                maxDepth: 3);
            foreach (var mi in menuItems)
            {
                TestContext?.WriteLine($"    {mi}");
            }

            // Try finding from WinAppDriver session root (not MainWindow)
            TestContext?.WriteLine("  Searching from WinAppDriver session root...");
            try
            {
                var openFromRoot = app.WinAppDriver.FindElementByName("Open");
                TestContext?.WriteLine($"  FOUND 'Open' from root! ClassName: {openFromRoot.GetAttribute("ClassName")}");
            }
            catch
            {
                TestContext?.WriteLine("  'Open' NOT found from session root either.");
            }

            // Dismiss context menu
            toolElement.SendKeys(OpenQA.Selenium.Keys.Escape);
            WaitingUtils.Wait(1000);

            // Step 5b: Try double-click as before
            TestContext?.WriteLine("Step 5b: Double-clicking the tool element...");
            actions = new OpenQA.Selenium.Interactions.Actions(app.WinAppDriver);
            actions.DoubleClick(toolElement).Perform();
            WaitingUtils.Wait(2000);
            toolElement.SendKeys(OpenQA.Selenium.Keys.Enter);
            WaitingUtils.Wait(10000);

            // Dump tree after double-click
            var step5Path = Path.Combine(resultsDir, "DiagStep5_AfterDoubleClick.txt");
            var count5 = UiTreeInspector.DumpElementTree(app.MainWindow, step5Path, maxDepth: 3);
            TestContext?.WriteLine($"  Dumped {count5} elements to {step5Path}");
            TestContext?.AddResultFile(step5Path);

            // Check for GP pane
            TestContext?.WriteLine("Step 6: Checking for Geoprocessing pane...");
            OpenQA.Selenium.Appium.AppiumWebElement? gpPane = null;
            try
            {
                gpPane = app.MainWindow.FindElementByAccessibilityId("esri_geoprocessing_GeoprocessingDockPane");
            }
            catch
            {
                TestContext?.WriteLine("  GP pane NOT found by AccessibilityId 'esri_geoprocessing_GeoprocessingDockPane'.");
            }

            // Step 7: Try clicking the GP tab to activate it
            TestContext?.WriteLine("Step 7: Clicking GP tab to activate it...");
            try
            {
                var gpTab = app.MainWindow.FindElementByAccessibilityId("esri_geoprocessing_toolBoxesTab");
                if (gpTab != null)
                {
                    TestContext?.WriteLine($"  GP tab found: Name='{gpTab.GetAttribute("Name")}', ClassName='{gpTab.GetAttribute("ClassName")}'");
                    gpTab.Click();
                    WaitingUtils.Wait(3000);

                    // Dump tree AFTER clicking GP tab
                    var step7Path = Path.Combine(resultsDir, "DiagStep7_AfterGPTabClick.txt");
                    var count7 = UiTreeInspector.DumpElementTree(app.MainWindow, step7Path, maxDepth: 3);
                    TestContext?.WriteLine($"  Dumped {count7} elements to {step7Path}");
                    TestContext?.AddResultFile(step7Path);

                    // Search for ALL ToolWindow elements
                    TestContext?.WriteLine("  Searching for all ToolWindow elements...");
                    var toolWindows = UiTreeInspector.FindElements(
                        app.MainWindow,
                        (automationId, name, className) =>
                            className.Contains("ToolWindow") ||
                            className.Contains("DockPane") ||
                            (automationId.Contains("eoprocessing") && !automationId.Contains("Button")),
                        maxDepth: 3);
                    foreach (var tw in toolWindows)
                    {
                        TestContext?.WriteLine($"    {tw}");
                    }

                    // Try finding GP pane again
                    try
                    {
                        gpPane = app.MainWindow.FindElementByAccessibilityId("esri_geoprocessing_GeoprocessingDockPane");
                        TestContext?.WriteLine("  GP pane FOUND after tab click!");
                    }
                    catch
                    {
                        TestContext?.WriteLine("  GP pane still NOT found after tab click.");

                        // Try alternative IDs
                        string[] altIds = [
                            "esri_geoprocessing_toolBoxes",
                            "esri_geoprocessing_ToolboxesDockPane",
                            "esri_geoprocessing_toolBoxesDockPane",
                            "esri_geoprocessing_GeoprocessingPane"
                        ];
                        foreach (var altId in altIds)
                        {
                            try
                            {
                                var alt = app.MainWindow.FindElementByAccessibilityId(altId);
                                if (alt != null)
                                {
                                    TestContext?.WriteLine($"  FOUND alternate: {altId} (Name='{alt.GetAttribute("Name")}', Class='{alt.GetAttribute("ClassName")}')");
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    TestContext?.WriteLine("  GP tab element is null.");
                }
            }
            catch (Exception ex)
            {
                TestContext?.WriteLine($"  GP tab not found: {ex.Message}");
            }

            if (gpPane != null)
            {
                TestContext?.WriteLine("  GP pane FOUND!");
                TestContext?.WriteLine($"    Name: {gpPane.GetAttribute("Name")}");
                TestContext?.WriteLine($"    ClassName: {gpPane.GetAttribute("ClassName")}");
            }
            else
            {
                TestContext?.WriteLine("  Searching for any Geoprocessing-related elements...");
                var gpElements = UiTreeInspector.FindElements(
                    app.MainWindow,
                    (automationId, name, className) =>
                        name.Contains("eoprocessing", StringComparison.OrdinalIgnoreCase) ||
                        automationId.Contains("eoprocessing", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Run", StringComparison.OrdinalIgnoreCase),
                    maxDepth: 3);
                foreach (var elem in gpElements)
                {
                    TestContext?.WriteLine($"    {elem}");
                }
            }
        }
        else
        {
            TestContext?.WriteLine("  Tool element NOT found. Searching all elements with 'Indoor' or 'Location'...");
            var matches = UiTreeInspector.FindElements(
                app.MainWindow,
                (automationId, name, className) =>
                    name.Contains("Indoor", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Location", StringComparison.OrdinalIgnoreCase) ||
                    automationId.Contains("Indoor", StringComparison.OrdinalIgnoreCase),
                maxDepth: 3);
            foreach (var elem in matches)
            {
                TestContext?.WriteLine($"    {elem}");
            }
        }
    }

    /// <summary>
    /// Attempts to find the Catalog pane using known AutomationId candidates.
    /// </summary>
    private static OpenQA.Selenium.Appium.AppiumWebElement? TryFindCatalogPane(
        ServiceNow.TestHelpers.ProApplication.Application app)
    {
        string[] catalogPaneIds =
        [
            "esri_core_projectDockPane",
            "esri_core_ProjectDockPane",
            "esri_core_CatalogDockPane",
        ];

        foreach (var paneId in catalogPaneIds)
        {
            var pane = WaitingUtils.RetryAssignmentUntilSuccess(
                () => app.MainWindow.FindElementByAccessibilityId(paneId),
                timeoutMs: 3000,
                debugInfo: $"TryFindCatalogPane — trying {paneId}");

            if (pane != null) return pane;
        }

        return null;
    }

    /// <summary>
    /// Attempts to open the Catalog pane via the View ribbon tab.
    /// </summary>
    private static void TryOpenCatalogViaViewTab(
        ServiceNow.TestHelpers.ProApplication.Application app)
    {
        var ribbon = app.MainWindow.FindElementByAccessibilityId("NewRibbon");
        var viewTab = WaitingUtils.RetryAssignmentUntilSuccess(
            () => ribbon.FindElementsByClassName("RibbonTabHeader")
                .FirstOrDefault(e => (e.GetAttribute("AutomationId") ?? "").Contains("iewTab")
                    || (e.GetAttribute("Name") ?? "").Equals("View", StringComparison.OrdinalIgnoreCase)),
            timeoutMs: 10000,
            debugInfo: "TryOpenCatalogViaViewTab — looking for View tab");

        viewTab?.Click();
        WaitingUtils.Wait(1000);

        string[] catalogButtonIds =
        [
            "esri_core_showProjectDockPane",
            "esri_core_showCatalogDockPane",
        ];

        foreach (var buttonId in catalogButtonIds)
        {
            try
            {
                var button = app.MainWindow.FindElementByAccessibilityId(buttonId);
                if (button != null)
                {
                    button.Click();
                    WaitingUtils.Wait(2000);
                    return;
                }
            }
            catch { }
        }
    }
}
