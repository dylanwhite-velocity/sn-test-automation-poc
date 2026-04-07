using ServiceNow.TestHelpers.ProApplication.Pane;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Ribbon;

/// <summary>
/// Page Object for the Analysis ribbon tab in ArcGIS Pro.
/// Mirrors the CUIT <c>AnalysisTab</c> — provides access to the Geoprocessing
/// pane, Python window, and other analysis tools.
/// </summary>
public class AnalysisTab : RibbonTabBase
{
    /// <summary>AutomationId for the Geoprocessing tools button on the Analysis tab.</summary>
    public const string GeoprocessingButtonId = "esri_geoprocessing_toolsButton";

    /// <summary>AutomationId for the History button on the Analysis tab.</summary>
    public const string HistoryButtonId = "esri_geoprocessing_showToolHistory";

    /// <summary>AutomationId for the Environments button on the Analysis tab.</summary>
    public const string EnvironmentsButtonId = "esri_geoprocessing_environmentsButton";

    /// <summary>
    /// Creates an AnalysisTab POM. The Analysis tab AutomationId contains "nalysisTab".
    /// </summary>
    public AnalysisTab(Application app) : base(app, "nalysisTab")
    {
    }

    /// <summary>
    /// Clicks the Geoprocessing button to open the Geoprocessing pane.
    /// </summary>
    /// <returns>A <see cref="GeoprocessingPane"/> POM for the opened pane.</returns>
    public GeoprocessingPane OpenGeoprocessing()
    {
        EnableTab();

        var button = WaitingUtils.RetryAssignmentUntilSuccess(
            () => App.MainWindow.FindElementByAccessibilityId(GeoprocessingButtonId),
            timeoutMs: 10000,
            debugInfo: "AnalysisTab.OpenGeoprocessing — looking for GP button");

        button?.Click();
        return new GeoprocessingPane(App);
    }
}
