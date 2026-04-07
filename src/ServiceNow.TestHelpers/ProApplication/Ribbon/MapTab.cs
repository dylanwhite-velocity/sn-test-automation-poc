namespace ServiceNow.TestHelpers.ProApplication.Ribbon;

/// <summary>
/// Page Object for the Map ribbon tab in ArcGIS Pro.
/// </summary>
public class MapTab : RibbonTabBase
{
    /// <summary>
    /// Creates a MapTab POM. The Map tab AutomationId contains "MapTab".
    /// </summary>
    public MapTab(Application app) : base(app, "MapTab")
    {
    }
}
