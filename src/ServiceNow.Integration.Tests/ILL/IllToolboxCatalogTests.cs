using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.ProApplication.Pane;
using ServiceNow.TestHelpers.ProApplication.Ribbon;

namespace ServiceNow.Integration.Tests.ILL;

/// <summary>
/// Tests that verify the ILL (Indoor Location Loader) Python Toolbox is accessible
/// in the ArcGIS Pro Catalog pane and that the IndoorsLocationLoader tool can be
/// opened from it.
///
/// <para>Validates:
/// <list type="number">
///   <item>The "Indoors ServiceNow Tools" toolbox is registered in the project and
///         visible under Catalog → Toolboxes.</item>
///   <item>The "IndoorsLocationLoader" script tool within the toolbox can be
///         double-clicked to open its dialog in the Geoprocessing pane.</item>
/// </list>
/// </para>
///
/// <para><strong>Prerequisites:</strong>
/// <list type="bullet">
///   <item>ArcGIS Pro installed with the project at <see cref="TestProjectPath"/>.</item>
///   <item>The Python Toolbox <c>Indoors ServiceNow Tools.pyt</c> must be added to the
///         project's Toolboxes (via Insert Toolbox or Project → Toolboxes → Add).</item>
///   <item>WinAppDriver is started automatically by <see cref="TestEnvironment"/>.</item>
/// </list>
/// </para>
///
/// <para>Run with: <c>dotnet test --filter "FullyQualifiedName~IllToolboxCatalogTests"</c></para>
/// </summary>
[TestClass]
public class IllToolboxCatalogTests : ServiceNowTestBase
{
    /// <summary>MSTest context for logging and result attachments.</summary>
    public new TestContext? TestContext { get; set; }

    /// <summary>
    /// Path to the ArcGIS Pro project file that has the ILL toolbox registered.
    /// </summary>
    private const string TestProjectPath =
        @"C:\Users\dyl13740\Documents\ArcGIS\Projects\MyProject\MyProject.aprx";

    /// <summary>
    /// Display name of the ILL Python Toolbox as shown in the Catalog pane.
    /// Discovered via CatalogInspectionTests: ArcGIS Pro shows the full file name
    /// including the .pyt extension in the Catalog breadcrumb and list.
    /// </summary>
    private const string ToolboxName = "Indoors ServiceNow Tools.pyt";

    /// <summary>
    /// Display name of the IndoorsLocationLoader script tool within the toolbox.
    /// Discovered via CatalogInspectionTests: ArcGIS Pro displays the tool label
    /// with spaces ("Indoors Location Loader"), not the Python class name.
    /// </summary>
    private const string ToolName = "Indoors Location Loader";

    /// <summary>
    /// Verifies that the ILL Python Toolbox is registered in the ArcGIS Pro project
    /// and visible under Catalog → Toolboxes.
    ///
    /// <para>This test opens ArcGIS Pro with the test project, navigates to the
    /// Catalog pane, expands the Toolboxes node, and asserts that "Indoors ServiceNow Tools"
    /// is listed.</para>
    ///
    /// <para>Related: <see href="https://github.com/EsriPS/ServiceNow_Esri_Integration">
    /// ServiceNow_Esri_Integration ILL module</see></para>
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify the ILL Python Toolbox is visible under Catalog → Toolboxes in ArcGIS Pro")]
    public void VerifyToolboxExistsInCatalog()
    {
        // Arrange — launch Pro with the project containing the ILL toolbox
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        // Act — open the Catalog pane and check for the toolbox
        TestContext?.WriteLine("Opening Catalog pane via View tab...");
        var viewTab = new ViewTab(app);
        var catalogPane = viewTab.OpenCatalogPane();

        TestContext?.WriteLine($"Checking for toolbox: {ToolboxName}");
        bool toolboxExists = catalogPane.DoesToolboxExist(ToolboxName);

        // Assert
        Assert.IsTrue(toolboxExists,
            $"ILL Python Toolbox '{ToolboxName}' should be visible under Catalog → Toolboxes. " +
            "Ensure the toolbox is added to the project (Project → Toolboxes → Add Toolbox).");
    }

    /// <summary>
    /// Verifies that the IndoorsLocationLoader tool can be found in the Catalog
    /// and opened via the Geoprocessing pane.
    ///
    /// <para>This test combines Catalog verification with GP pane tool opening:
    /// <list type="number">
    ///   <item>Launches Pro → opens Catalog → navigates to Toolboxes →
    ///         verifies "Indoors Location Loader" exists as a tool.</item>
    ///   <item>Opens the tool via the Geoprocessing pane search (WinAppDriver
    ///         cannot double-click Catalog ListViewItems to open tools).</item>
    ///   <item>Verifies the tool dialog loaded (Run button present).</item>
    /// </list>
    /// </para>
    ///
    /// <para>Related: <see href="https://github.com/EsriPS/ServiceNow_Esri_Integration">
    /// ServiceNow_Esri_Integration ILL module</see></para>
    /// </summary>
    [TestMethod]
    [TestCategory("ILL")]
    [Description("Verify IndoorsLocationLoader tool exists in Catalog and opens in GP pane")]
    public void VerifyIndoorsLocationLoaderOpensFromCatalog()
    {
        // Arrange — launch Pro with the project
        TestContext?.WriteLine($"Launching ArcGIS Pro with project: {TestProjectPath}");
        var app = StartProWithProject(TestProjectPath);

        // Act — verify the tool exists in the Catalog pane
        TestContext?.WriteLine("Opening Catalog pane via View tab...");
        var viewTab = new ViewTab(app);
        var catalogPane = viewTab.OpenCatalogPane();

        TestContext?.WriteLine($"Verifying tool exists: {ToolboxName} → {ToolName}");
        bool toolExists = catalogPane.DoesToolExist(ToolboxName, ToolName);
        Assert.IsTrue(toolExists,
            $"The '{ToolName}' tool should exist under '{ToolboxName}' in the Catalog pane.");

        // Act — open the tool via GP pane search (most reliable WinAppDriver approach)
        TestContext?.WriteLine($"Opening tool via Geoprocessing pane search: {ToolName}");
        var gpPane = catalogPane.OpenToolViaGeoprocessingSearch(ToolName);

        // Assert — the tool dialog should have loaded in the Geoprocessing pane
        bool toolLoaded = gpPane.DidToolLoad();

        Assert.IsTrue(toolLoaded,
            $"The '{ToolName}' tool dialog should have loaded in the Geoprocessing pane. " +
            "If this fails, the tool may not be a valid script tool, or the GP pane search did not find it.");
    }
}
