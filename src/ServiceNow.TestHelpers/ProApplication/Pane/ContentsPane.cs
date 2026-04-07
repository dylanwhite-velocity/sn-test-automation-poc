using OpenQA.Selenium.Appium;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Page Object for the Contents pane in ArcGIS Pro.
/// The Contents pane displays the layer list for the active map.
/// Used to verify CDF feature layers are loaded and visible.
/// </summary>
public class ContentsPane : PaneBase
{
    /// <summary>AutomationId for the Contents pane.</summary>
    public const string ContentsPaneId = "esri_core_contentsDockPane";

    /// <summary>
    /// Creates a ContentsPane POM.
    /// </summary>
    public ContentsPane(Application app) : base(app, ContentsPaneId)
    {
    }

    /// <summary>
    /// Checks whether a layer with the given name exists in the Contents pane.
    /// </summary>
    /// <param name="layerName">The display name of the layer to find.</param>
    /// <returns><c>true</c> if the layer is found.</returns>
    public bool DoesLayerExist(string layerName)
    {
        if (PaneElement == null) return false;

        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    var layer = PaneElement.FindElementByName(layerName);
                    return layer != null;
                }
                catch
                {
                    return false;
                }
            },
            timeoutMs: 15000);
    }

    /// <summary>
    /// Clicks on a layer by name to select it in the Contents pane.
    /// </summary>
    /// <param name="layerName">The display name of the layer to select.</param>
    public void SelectLayer(string layerName)
    {
        if (PaneElement == null)
            throw new InvalidOperationException("Contents pane is not open.");

        var layer = WaitingUtils.RetryAssignmentUntilSuccess(
            () => PaneElement.FindElementByName(layerName),
            timeoutMs: 10000,
            debugInfo: $"ContentsPane.SelectLayer — looking for '{layerName}'");

        layer?.Click();
    }

    /// <summary>
    /// Gets the names of all visible layers in the Contents pane.
    /// </summary>
    public IReadOnlyList<string> GetLayerNames()
    {
        if (PaneElement == null) return Array.Empty<string>();

        try
        {
            var treeItems = PaneElement.FindElementsByClassName("TreeViewItem");
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
}
