using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.ProApplication.Pane;
using ServiceNow.TestHelpers.ProApplication.Ribbon;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.Integration.Tests.Discovery;

/// <summary>
/// Diagnostic tests that dump the accessibility properties of ILL (Indoor Location Loader)
/// GP tool parameters and compare them with a built-in GP tool. These tests investigate
/// whether ArcGIS Pro sets AutomationId/Name properties on Python Toolbox (.pyt) parameter
/// UI elements — critical for WinAppDriver test automation.
///
/// <para><strong>Background:</strong> The CUIT framework locates GP tool parameters by reading
/// the <c>AutomationId</c> of each parameter's Text label element. For the ILL Python Toolbox,
/// <c>AutomationId</c> is completely absent from the element property tree, and the ComboBox
/// <c>Name</c> shows "Property does not exist". This test suite captures the full UI tree
/// to establish ground truth for implementing a workaround.</para>
///
/// <para><strong>Output:</strong> Verbose accessibility dumps are saved to TestResults/.
/// Compare <c>IllToolAccessibilityTree.txt</c> with <c>BuiltinToolAccessibilityTree.txt</c>
/// to determine if the missing AutomationId is Python Toolbox-specific.</para>
///
/// <para>Run with: <c>dotnet test --filter "FullyQualifiedName~GpParameterAccessibilityTests"</c></para>
/// </summary>
[TestClass]
public class GpParameterAccessibilityTests : ServiceNowTestBase
{
    /// <summary>MSTest context for logging and output paths.</summary>
    public new TestContext? TestContext { get; set; }

    /// <summary>
    /// Path to an ArcGIS Pro project that has the ILL toolbox registered.
    /// </summary>
    private const string TestProjectPath =
        @"C:\Users\dyl13740\Documents\ArcGIS\Projects\ServiceNowIntegrationProject\ServiceNowIntegrationProject.aprx";

    /// <summary>
    /// Display name of the ILL tool as shown in the GP pane search results.
    /// </summary>
    private const string IllToolName = "Indoors Location Loader";

    /// <summary>
    /// A built-in ArcGIS Pro GP tool for comparison. Buffer is a good choice
    /// because it has multiple parameter types (feature layer input, distance, etc.).
    /// </summary>
    private const string BuiltinToolName = "Buffer";

    /// <summary>
    /// Dumps the full verbose accessibility tree for the ILL tool dialog in the GP pane.
    /// Captures AutomationId, Name, ClassName, ControlType, IsKeyboardFocusable, and
    /// BoundingRectangle for every element within the GP pane after the ILL tool is opened.
    ///
    /// <para>This test answers: Does ArcGIS Pro set AutomationId on the parameter label
    /// Text elements (e.g., "Input Facility Features") for our Python Toolbox? The CUIT
    /// framework expects <c>AutomationId = arcpy.Parameter.name</c> (e.g., "in_facility_features").</para>
    ///
    /// <para>Output: <c>TestResults/IllToolAccessibilityTree.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Dump accessibility properties for ILL Python Toolbox GP parameters")]
    public void DumpIllToolParameterAccessibility()
    {
        // Arrange — launch Pro and open the ILL tool
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        TestContext?.WriteLine("Opening Geoprocessing pane via Analysis tab...");
        var analysisTab = new AnalysisTab(app);
        var gpPane = analysisTab.OpenGeoprocessing();

        TestContext?.WriteLine($"Searching for tool: {IllToolName}");
        gpPane.SearchForTool(IllToolName);

        bool toolLoaded = gpPane.DidToolLoad();
        Assert.IsTrue(toolLoaded,
            $"The '{IllToolName}' tool dialog should have loaded in the GP pane. " +
            "Ensure the ILL Python Toolbox is added to the project.");

        // Act — dump the GP pane accessibility tree (verbose mode)
        var outputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "IllToolAccessibilityTree.txt");

        TestContext?.WriteLine($"Dumping verbose accessibility tree to: {outputPath}");

        // Find the GP pane element
        var gpPaneElement = WaitingUtils.RetryAssignmentUntilSuccess(
            () => app.MainWindow.FindElementByAccessibilityId(GeoprocessingPane.GeoprocessingPaneId),
            timeoutMs: 10000,
            debugInfo: "Finding GP pane for accessibility dump");
        Assert.IsNotNull(gpPaneElement, "GP pane element should be found");

        var elementCount = UiTreeInspector.DumpAccessibilityTree(gpPaneElement, outputPath);

        TestContext?.WriteLine($"Dumped {elementCount} elements");

        // Also do a targeted search for parameter-related elements
        TestContext?.WriteLine("--- Targeted Parameter Analysis ---");

        // Look for elements that might be parameter labels (ClassName = "Text")
        var textElements = UiTreeInspector.FindElements(gpPaneElement,
            (automationId, name, className) => className == "TextBlock" || className == "Text");
        TestContext?.WriteLine($"Found {textElements.Count} Text/TextBlock elements:");
        foreach (var el in textElements)
        {
            TestContext?.WriteLine($"  {el}");
        }

        // Look for ComboBox elements (parameter inputs)
        var comboElements = UiTreeInspector.FindElements(gpPaneElement,
            (automationId, name, className) => className == "ComboBox");
        TestContext?.WriteLine($"Found {comboElements.Count} ComboBox elements:");
        foreach (var el in comboElements)
        {
            TestContext?.WriteLine($"  {el}");
        }

        // Look for any element with AutomationId containing parameter names
        var paramNames = new[] { "in_facility_features", "in_level_features", "in_unit_features",
            "servicenow_rest_url", "servicenow_username", "servicenow_password", "keep_duplicate_value" };
        foreach (var paramName in paramNames)
        {
            var paramElements = UiTreeInspector.FindElements(gpPaneElement,
                (automationId, name, className) =>
                    automationId.Contains(paramName, StringComparison.OrdinalIgnoreCase));
            TestContext?.WriteLine($"Elements with AutomationId containing '{paramName}': {paramElements.Count}");
            foreach (var el in paramElements)
            {
                TestContext?.WriteLine($"  {el}");
            }
        }

        // Look for elements with displayName in their Name property
        var displayNames = new[] { "Input Facility Features", "Input Level Features", "Input Unit Features",
            "ServiceNow REST URL", "ServiceNow Username", "ServiceNow Password", "Keep Duplicate Value" };
        foreach (var displayName in displayNames)
        {
            var nameElements = UiTreeInspector.FindElements(gpPaneElement,
                (automationId, name, className) =>
                    name.Contains(displayName, StringComparison.OrdinalIgnoreCase));
            TestContext?.WriteLine($"Elements with Name containing '{displayName}': {nameElements.Count}");
            foreach (var el in nameElements)
            {
                TestContext?.WriteLine($"  {el}");
            }
        }

        // Assert — minimal check that we got elements
        Assert.IsTrue(elementCount > 0, "Should have found elements in the GP pane");
    }

    /// <summary>
    /// Dumps the full verbose accessibility tree for a built-in GP tool (Buffer) for comparison.
    /// This establishes a baseline: if built-in tools DO have AutomationId on parameter labels
    /// but the ILL .pyt does not, then the issue is Python Toolbox-specific.
    ///
    /// <para>Output: <c>TestResults/BuiltinToolAccessibilityTree.txt</c></para>
    /// </summary>
    [TestMethod]
    [TestCategory("Discovery")]
    [Description("Dump accessibility properties for built-in GP tool (Buffer) for comparison")]
    public void DumpBuiltinToolParameterAccessibility()
    {
        // Arrange — launch Pro and open the Buffer tool
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        TestContext?.WriteLine("Opening Geoprocessing pane via Analysis tab...");
        var analysisTab = new AnalysisTab(app);
        var gpPane = analysisTab.OpenGeoprocessing();

        TestContext?.WriteLine($"Searching for tool: {BuiltinToolName}");
        gpPane.SearchForTool(BuiltinToolName);

        bool toolLoaded = gpPane.DidToolLoad();
        Assert.IsTrue(toolLoaded,
            $"The '{BuiltinToolName}' tool dialog should have loaded in the GP pane.");

        // Act — dump the GP pane accessibility tree (verbose mode)
        var outputPath = Path.Combine(
            TestContext?.TestResultsDirectory ?? ".",
            "BuiltinToolAccessibilityTree.txt");

        TestContext?.WriteLine($"Dumping verbose accessibility tree to: {outputPath}");

        var gpPaneElement = WaitingUtils.RetryAssignmentUntilSuccess(
            () => app.MainWindow.FindElementByAccessibilityId(GeoprocessingPane.GeoprocessingPaneId),
            timeoutMs: 10000,
            debugInfo: "Finding GP pane for accessibility dump");
        Assert.IsNotNull(gpPaneElement, "GP pane element should be found");

        var elementCount = UiTreeInspector.DumpAccessibilityTree(gpPaneElement, outputPath);

        TestContext?.WriteLine($"Dumped {elementCount} elements");

        // Targeted search for Buffer's known parameter names
        var bufferParamNames = new[] { "in_features", "out_feature_class", "buffer_distance_or_field" };
        foreach (var paramName in bufferParamNames)
        {
            var paramElements = UiTreeInspector.FindElements(gpPaneElement,
                (automationId, name, className) =>
                    automationId.Contains(paramName, StringComparison.OrdinalIgnoreCase));
            TestContext?.WriteLine($"Elements with AutomationId containing '{paramName}': {paramElements.Count}");
            foreach (var el in paramElements)
            {
                TestContext?.WriteLine($"  {el}");
            }
        }

        // Look for Text elements to see their AutomationId pattern
        var textElements = UiTreeInspector.FindElements(gpPaneElement,
            (automationId, name, className) => className == "TextBlock" || className == "Text");
        TestContext?.WriteLine($"Found {textElements.Count} Text/TextBlock elements:");
        foreach (var el in textElements)
        {
            TestContext?.WriteLine($"  {el}");
        }

        Assert.IsTrue(elementCount > 0, "Should have found elements in the GP pane");
    }
}
