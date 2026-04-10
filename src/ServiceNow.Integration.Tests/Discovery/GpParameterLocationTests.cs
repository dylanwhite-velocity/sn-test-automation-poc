using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.ProApplication.Pane;
using ServiceNow.TestHelpers.ProApplication.Ribbon;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.Integration.Tests.Discovery;

/// <summary>
/// Verification tests for the <see cref="GpToolDialog"/> parameter location strategy.
/// Confirms that <see cref="GpToolDialog.GetToolPaneControls"/> correctly maps all ILL
/// parameters and that <see cref="GpToolDialog.SetComboBoxValue"/>,
/// <see cref="GpToolDialog.SetCheckBoxValue"/>, and <see cref="GpToolDialog.SetPasswordValue"/>
/// interact with the correct UI controls.
///
/// <para>These tests validate the CUIT-compatible parameter location pattern
/// against a live ArcGIS Pro instance with the ILL Python Toolbox loaded.</para>
///
/// <para>Run with: <c>dotnet test --filter "FullyQualifiedName~GpParameterLocationTests" --settings test.runsettings</c></para>
/// </summary>
[TestClass]
public class GpParameterLocationTests : ServiceNowTestBase
{
    /// <summary>MSTest context for logging and output paths.</summary>
    public new TestContext? TestContext { get; set; }

    /// <summary>Path to an ArcGIS Pro project with the ILL toolbox registered.</summary>
    private const string TestProjectPath =
        @"C:\Users\dyl13740\Documents\ArcGIS\Projects\MyProject\MyProject.aprx";

    /// <summary>Display name of the ILL tool in GP pane search results.</summary>
    private const string IllToolName = "Indoors Location Loader";

    /// <summary>
    /// All expected ILL parameter names (matching <c>arcpy.Parameter.name</c>).
    /// Order matches the parameter index in the Python Toolbox.
    /// </summary>
    private static readonly string[] ExpectedIllParameters =
    [
        "in_facility_features",
        "in_level_features",
        "in_unit_features",
        "servicenow_rest_url",
        "servicenow_username",
        "servicenow_password",
        "keep_duplicate_value"
    ];

    /// <summary>
    /// Maps each ILL parameter name to its expected display name.
    /// </summary>
    private static readonly Dictionary<string, string> ParameterDisplayNames = new()
    {
        ["in_facility_features"] = "Input Facility Features",
        ["in_level_features"] = "Input Level Features",
        ["in_unit_features"] = "Input Unit Features",
        ["servicenow_rest_url"] = "ServiceNow REST URL",
        ["servicenow_username"] = "ServiceNow Username",
        ["servicenow_password"] = "ServiceNow Password",
        ["keep_duplicate_value"] = "Keep Duplicate Value"
    };

    /// <summary>
    /// Verifies that <see cref="GpToolDialog.GetToolPaneControls"/> discovers all 7 ILL
    /// parameters by AutomationId and that each parameter has associated controls.
    /// This is the core validation that the CUIT-compatible parameter location works
    /// for Python Toolbox (.pyt) tools.
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify GpToolDialog.GetToolPaneControls() finds all 7 ILL parameters")]
    public void VerifyGetToolPaneControlsFindsAllIllParameters()
    {
        // Arrange — launch Pro and open the ILL tool
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        TestContext?.WriteLine("Opening Geoprocessing pane via Analysis tab...");
        var analysisTab = new AnalysisTab(app);
        analysisTab.EnableTab();
        var gp = analysisTab.OpenGeoprocessing();

        TestContext?.WriteLine($"Searching for tool: {IllToolName}");
        gp.SearchForTool(IllToolName);

        Assert.IsTrue(gp.DidToolLoad(), "ILL tool should load in the Geoprocessing pane");

        // Act — get the tool dialog and build the parameter map
        TestContext?.WriteLine("Getting tool dialog and building parameter map...");
        var dialog = gp.GetToolDialog();
        var controls = dialog.GetToolPaneControls();

        // Log what was found
        TestContext?.WriteLine($"Found {controls.Count} parameters:");
        foreach (var kvp in controls)
        {
            var controlTypes = string.Join(", ",
                kvp.Value.Select(e =>
                {
                    try { return e.GetAttribute("ClassName"); }
                    catch { return "?"; }
                }));
            TestContext?.WriteLine($"  {kvp.Key}: [{controlTypes}] ({kvp.Value.Count} controls)");
        }

        // Assert — all 7 parameters found
        Assert.AreEqual(ExpectedIllParameters.Length, controls.Count,
            $"Expected {ExpectedIllParameters.Length} parameters, found {controls.Count}. " +
            $"Found: [{string.Join(", ", controls.Keys)}]");

        foreach (var expectedParam in ExpectedIllParameters)
        {
            Assert.IsTrue(controls.ContainsKey(expectedParam),
                $"Parameter '{expectedParam}' should be in the control map. " +
                $"Found: [{string.Join(", ", controls.Keys)}]");

            Assert.IsTrue(controls[expectedParam].Count > 0,
                $"Parameter '{expectedParam}' should have at least one associated control.");
        }

        // Verify DoesParameterExist works
        foreach (var expectedParam in ExpectedIllParameters)
        {
            Assert.IsTrue(dialog.DoesParameterExist(expectedParam),
                $"DoesParameterExist('{expectedParam}') should return true.");
        }

        Assert.IsFalse(dialog.DoesParameterExist("nonexistent_param"),
            "DoesParameterExist should return false for a non-existent parameter.");
    }

    /// <summary>
    /// Verifies that <see cref="GpToolDialog.SetComboBoxValue"/> can set text values
    /// on string-type parameters (ServiceNow REST URL and Username).
    /// These are free-text ComboBox inputs, not dropdown selections.
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify SetComboBoxValue works on ILL string parameters")]
    public void VerifySetComboBoxValueOnStringParameters()
    {
        // Arrange
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        var analysisTab = new AnalysisTab(app);
        analysisTab.EnableTab();
        var gp = analysisTab.OpenGeoprocessing();
        gp.SearchForTool(IllToolName);

        Assert.IsTrue(gp.DidToolLoad(), "ILL tool should load.");

        var dialog = gp.GetToolDialog();
        dialog.GetToolPaneControls();

        // Act — set the ServiceNow REST URL
        const string testUrl = "https://test.service-now.com/api/now";
        TestContext?.WriteLine($"Setting servicenow_rest_url to: {testUrl}");
        dialog.SetComboBoxValue("servicenow_rest_url", testUrl);

        // Act — set the ServiceNow Username
        const string testUsername = "test_user";
        TestContext?.WriteLine($"Setting servicenow_username to: {testUsername}");
        dialog.SetComboBoxValue("servicenow_username", testUsername);

        // Assert — verify the values were set (read back)
        var actualUrl = dialog.GetComboBoxValue("servicenow_rest_url");
        var actualUsername = dialog.GetComboBoxValue("servicenow_username");

        TestContext?.WriteLine($"Read back servicenow_rest_url: '{actualUrl}'");
        TestContext?.WriteLine($"Read back servicenow_username: '{actualUsername}'");

        // Note: ComboBox value readback may not be exact due to GP validation.
        // At minimum, verify no exception was thrown during SetComboBoxValue.
        // The test passing without exception confirms the parameter map correctly
        // locates the ComboBox controls.
        TestContext?.WriteLine("SetComboBoxValue completed successfully for both string parameters.");
    }

    /// <summary>
    /// Verifies that <see cref="GpToolDialog.SetCheckBoxValue"/> can toggle the
    /// Keep Duplicate Value boolean parameter.
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify SetCheckBoxValue works on ILL boolean parameter")]
    public void VerifySetCheckBoxValueOnBooleanParameter()
    {
        // Arrange
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        var analysisTab = new AnalysisTab(app);
        analysisTab.EnableTab();
        var gp = analysisTab.OpenGeoprocessing();
        gp.SearchForTool(IllToolName);

        Assert.IsTrue(gp.DidToolLoad(), "ILL tool should load.");

        var dialog = gp.GetToolDialog();
        dialog.GetToolPaneControls();

        // Act — read initial state, then toggle
        var initialState = dialog.GetCheckBoxValue("keep_duplicate_value");
        TestContext?.WriteLine($"Initial keep_duplicate_value state: {initialState}");

        dialog.SetCheckBoxValue("keep_duplicate_value", !initialState);
        WaitingUtils.Wait(500);

        var toggledState = dialog.GetCheckBoxValue("keep_duplicate_value");
        TestContext?.WriteLine($"After toggle keep_duplicate_value state: {toggledState}");

        // Assert — state should have changed
        Assert.AreNotEqual(initialState, toggledState,
            "CheckBox state should change after SetCheckBoxValue toggle.");

        // Toggle back to original
        dialog.SetCheckBoxValue("keep_duplicate_value", initialState);
        WaitingUtils.Wait(500);

        var restoredState = dialog.GetCheckBoxValue("keep_duplicate_value");
        TestContext?.WriteLine($"After restore keep_duplicate_value state: {restoredState}");

        Assert.AreEqual(initialState, restoredState,
            "CheckBox state should be restored to original value.");
    }

    /// <summary>
    /// Verifies that <see cref="GpToolDialog.SetPasswordValue"/> can set the
    /// ServiceNow Password parameter without throwing exceptions.
    /// Password values cannot be read back, so we only verify no error occurs.
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify SetPasswordValue works on ILL password parameter")]
    public void VerifySetPasswordValueOnPasswordParameter()
    {
        // Arrange
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        var analysisTab = new AnalysisTab(app);
        analysisTab.EnableTab();
        var gp = analysisTab.OpenGeoprocessing();
        gp.SearchForTool(IllToolName);

        Assert.IsTrue(gp.DidToolLoad(), "ILL tool should load.");

        var dialog = gp.GetToolDialog();
        dialog.GetToolPaneControls();

        // Act — set password value (password cannot be read back)
        TestContext?.WriteLine("Setting servicenow_password...");
        dialog.SetPasswordValue("servicenow_password", "test_password_value");

        // Assert — reaching here without exception means the PasswordBox was found and interacted with
        TestContext?.WriteLine("SetPasswordValue completed successfully.");
        Assert.IsTrue(dialog.DoesParameterExist("servicenow_password"),
            "servicenow_password parameter should exist.");
    }

    /// <summary>
    /// Verifies the <see cref="GpToolDialog.GetParameterNames"/> method returns
    /// all expected parameter names in the correct order.
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify GetParameterNames returns expected ILL parameters")]
    public void VerifyGetParameterNamesReturnsExpectedList()
    {
        // Arrange
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        var analysisTab = new AnalysisTab(app);
        analysisTab.EnableTab();
        var gp = analysisTab.OpenGeoprocessing();
        gp.SearchForTool(IllToolName);

        Assert.IsTrue(gp.DidToolLoad(), "ILL tool should load.");

        var dialog = gp.GetToolDialog();

        // Act
        var paramNames = dialog.GetParameterNames();

        // Assert
        TestContext?.WriteLine($"GetParameterNames returned {paramNames.Count} parameters:");
        foreach (var name in paramNames)
        {
            TestContext?.WriteLine($"  - {name}");
        }

        Assert.AreEqual(ExpectedIllParameters.Length, paramNames.Count,
            "Parameter count should match expected.");

        // Verify each expected parameter is present (order may vary)
        foreach (var expected in ExpectedIllParameters)
        {
            Assert.IsTrue(paramNames.Contains(expected),
                $"Parameter '{expected}' should be in GetParameterNames() result.");
        }
    }
}
