namespace ServiceNow.TestHelpers.ProApplication.Ribbon;

/// <summary>
/// Page Object for the Insert ribbon tab in ArcGIS Pro.
/// </summary>
public class InsertTab : RibbonTabBase
{
    /// <summary>
    /// Creates an InsertTab POM. The Insert tab AutomationId contains "InsertTab".
    /// </summary>
    public InsertTab(Application app) : base(app, "InsertTab")
    {
    }
}
