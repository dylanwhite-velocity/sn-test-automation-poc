using OpenQA.Selenium.Appium;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Ribbon;

/// <summary>
/// Base class for all ArcGIS Pro ribbon tab Page Objects.
/// Mirrors the CUIT <c>RibbonTabBase</c> pattern.
///
/// <para>Each ribbon tab (Analysis, Map, Insert, etc.) extends this class
/// and provides its own <c>TabAutomationId</c>.</para>
/// </summary>
public abstract class RibbonTabBase
{
    /// <summary>AutomationId of the ArcGIS Pro ribbon control.</summary>
    private const string RibbonAutomationId = "NewRibbon";

    private AppiumWebElement? _tab;

    /// <summary>
    /// Creates a ribbon tab POM for the specified tab AutomationId.
    /// </summary>
    /// <param name="app">The running ArcGIS Pro application POM.</param>
    /// <param name="tabAutomationId">The AutomationId substring that identifies this tab.</param>
    protected RibbonTabBase(Application app, string tabAutomationId)
    {
        App = app;
        TabAutomationId = tabAutomationId;
        Ribbon = app.MainWindow.FindElementByAccessibilityId(RibbonAutomationId);
    }

    /// <summary>The ArcGIS Pro Application POM.</summary>
    protected Application App { get; }

    /// <summary>The AutomationId substring for this tab.</summary>
    protected string TabAutomationId { get; set; }

    /// <summary>The ribbon element.</summary>
    protected AppiumWebElement Ribbon { get; }

    /// <summary>
    /// The tab header element. Lazy-loaded by searching for a <c>RibbonTabHeader</c>
    /// whose AutomationId contains <see cref="TabAutomationId"/>.
    /// </summary>
    public AppiumWebElement? Tab
    {
        get => _tab ??= Ribbon.FindElementsByClassName("RibbonTabHeader")
            .FirstOrDefault(e => (e.GetAttribute("AutomationId") ?? "").Contains(TabAutomationId));
        set => _tab = value;
    }

    /// <summary>Returns <c>true</c> if the tab exists in the ribbon.</summary>
    public bool DoesTabExist() => Tab != null;

    /// <summary>
    /// Clicks the tab header to activate (select) this ribbon tab.
    /// </summary>
    public void EnableTab()
    {
        var tab = WaitingUtils.RetryAssignmentUntilSuccess(
            () => Ribbon.FindElementsByClassName("RibbonTabHeader")
                .FirstOrDefault(e => (e.GetAttribute("AutomationId") ?? "").Contains(TabAutomationId)),
            timeoutMs: 10000,
            debugInfo: $"RibbonTabBase.EnableTab — looking for {TabAutomationId}");

        tab?.Click();
        _tab = tab;
    }
}
