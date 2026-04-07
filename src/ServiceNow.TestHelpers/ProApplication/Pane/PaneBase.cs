using OpenQA.Selenium.Appium;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Base class for all ArcGIS Pro dockable pane Page Objects.
/// Mirrors the CUIT <c>PaneBase</c> pattern.
///
/// <para>Each pane (Contents, Catalog, Geoprocessing, etc.) extends this class
/// and provides its own <c>PaneAutomationId</c>.</para>
/// </summary>
public abstract class PaneBase
{
    /// <summary>
    /// Creates a pane POM by finding the pane element in the main window.
    /// </summary>
    /// <param name="app">The running ArcGIS Pro application POM.</param>
    /// <param name="paneAutomationId">The AutomationId of this pane.</param>
    protected PaneBase(Application app, string paneAutomationId)
    {
        App = app;
        PaneAutomationId = paneAutomationId;

        PaneElement = WaitingUtils.RetryAssignmentUntilSuccess(
            () => app.MainWindow.FindElementByAccessibilityId(paneAutomationId),
            timeoutMs: 15000,
            debugInfo: $"PaneBase constructor — looking for pane {paneAutomationId}");
    }

    /// <summary>The ArcGIS Pro Application POM.</summary>
    protected Application App { get; }

    /// <summary>The AutomationId for this pane.</summary>
    protected string PaneAutomationId { get; }

    /// <summary>The root element of this pane. May be null if pane is not open.</summary>
    protected AppiumWebElement? PaneElement { get; set; }

    /// <summary>Returns <c>true</c> if the pane is currently visible.</summary>
    public bool IsPaneOpen()
    {
        try
        {
            return PaneElement != null && !PaneElement.GetAttribute("IsOffscreen").Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
