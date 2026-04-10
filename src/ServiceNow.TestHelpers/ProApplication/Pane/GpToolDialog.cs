using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.TestHelpers.ProApplication.Pane;

/// <summary>
/// Page Object for a loaded Geoprocessing tool dialog inside the GP pane.
/// Mirrors the CUIT <c>gp_tool_dialog</c> class and its
/// <c>GetToolPaneControls()</c> algorithm (gp_tool_dialog.cs:454-566).
///
/// <para>The core pattern: walk child elements of the tool dialog's ScrollViewer.
/// Each <c>Text</c> element's <c>AutomationId</c> is the parameter name
/// (matching <c>arcpy.Parameter.name</c>). All subsequent non-label elements
/// until the next <c>Text</c> are that parameter's input controls (ComboBox,
/// PasswordBox, Button, etc.).</para>
///
/// <para><b>Important:</b> GP parameter input controls (ComboBox, PasswordBox)
/// do NOT have AutomationId. They are located through this positional map,
/// not by direct accessibility lookup. This behavior is identical for both
/// built-in and Python Toolbox (.pyt) tools.</para>
/// </summary>
public class GpToolDialog
{
    private readonly Application _app;

    /// <summary>
    /// AutomationId of the tool dialog page inside the GP pane.
    /// </summary>
    public const string ToolDialogAutomationId = "gp_tool_dialog";

    /// <summary>
    /// AutomationId of the Run button.
    /// </summary>
    public const string RunButtonAutomationId = "run_btn";

    /// <summary>
    /// Creates a GpToolDialog wrapping a loaded tool dialog element.
    /// </summary>
    /// <param name="app">The ArcGIS Pro application POM.</param>
    /// <param name="toolDialogElement">The ToolDialogPage element (AutomationId: gp_tool_dialog).</param>
    public GpToolDialog(Application app, AppiumWebElement toolDialogElement)
    {
        _app = app;
        ToolPane = toolDialogElement;

        ScrollToolPane = WaitingUtils.RetryAssignmentUntilSuccess(
            () => ToolPane.FindElementByClassName("ScrollViewer"),
            timeoutMs: 10000,
            debugInfo: "GpToolDialog — looking for ScrollViewer inside ToolDialogPage");
    }

    /// <summary>The ToolDialogPage element (AutomationId: gp_tool_dialog).</summary>
    public AppiumWebElement ToolPane { get; }

    /// <summary>The ScrollViewer that contains all parameter elements.</summary>
    public AppiumWebElement? ScrollToolPane { get; }

    /// <summary>
    /// Cached parameter control map. Populated by <see cref="GetToolPaneControls"/>.
    /// Key = parameter name (AutomationId of Text label), Value = list of associated controls.
    /// </summary>
    public Dictionary<string, List<AppiumWebElement>>? ToolControls { get; private set; }

    /// <summary>
    /// Builds a dictionary mapping parameter names to their associated UI controls.
    /// Implements a simplified version of CUIT's <c>GetToolPaneControls()</c>.
    ///
    /// <para>Algorithm:</para>
    /// <list type="number">
    ///   <item>Enumerate direct children of the ScrollViewer</item>
    ///   <item>When a <c>Text</c> element is found, its <c>AutomationId</c> becomes the current parameter key</item>
    ///   <item>When a <c>CheckBox</c> is found, the first child Text's <c>AutomationId</c> is the key; the CheckBox itself is the control</item>
    ///   <item>When an <c>Image</c> with AutomationId <c>ParameterStatus</c> is found, it's added to the current parameter or buffered</item>
    ///   <item>All other elements are added as controls for the current parameter</item>
    /// </list>
    /// </summary>
    /// <returns>Dictionary mapping parameter names to their controls.</returns>
    public Dictionary<string, List<AppiumWebElement>> GetToolPaneControls()
    {
        if (ScrollToolPane == null)
            throw new InvalidOperationException("ScrollViewer not found in tool dialog.");

        // Reset cached map
        ToolControls = null;

        var toolMapping = new Dictionary<string, List<AppiumWebElement>>();
        string? currentLabelAutomationId = null;
        var statusImages = new List<AppiumWebElement>();

        // Known element types that participate in the parameter structure.
        // We use ".//*" to get ALL descendants of the ScrollViewer (the WPF
        // visual tree has varying depth between ScrollViewer and the actual
        // parameter elements — intermediate containers like ContentPresenter,
        // StackPanel, Grid, etc. make "*/*" unreliable).
        var knownClassNames = new HashSet<string>
        {
            "Text", "TextBlock", "ComboBox", "Image", "Button",
            "CheckBox", "PasswordBox", "TextBox"
        };

        var allDescendants = ScrollToolPane.FindElementsByXPath(".//*");

        Trace.WriteLine($"[GpToolDialog] ScrollViewer has {allDescendants.Count} total descendants");

        // Filter to known parameter element types
        var parameterElements = new List<AppiumWebElement>();
        foreach (var element in allDescendants)
        {
            var className = GetClassName(element);
            if (knownClassNames.Contains(className))
                parameterElements.Add(element);
        }

        Trace.WriteLine($"[GpToolDialog] Filtered to {parameterElements.Count} known-type elements");

        foreach (var element in parameterElements)
        {
            var className = GetClassName(element);

            // ParameterStatus images — buffer until we know which parameter they belong to
            if (className == "Image")
            {
                var automationId = GetAutomationId(element);
                if (automationId == "ParameterStatus")
                {
                    statusImages.Add(element);
                }
                else if (currentLabelAutomationId != null && toolMapping.ContainsKey(currentLabelAutomationId))
                {
                    toolMapping[currentLabelAutomationId].Add(element);
                }
                continue;
            }

            // CheckBox — special structure: the Text label is nested INSIDE the CheckBox
            if (className == "CheckBox")
            {
                var checkBoxAutoId = GetAutomationId(element);

                // Try to get the label from the CheckBox's child Text element
                try
                {
                    var childText = element.FindElementByClassName("Text");
                    var childAutoId = GetAutomationId(childText);
                    if (!string.IsNullOrEmpty(childAutoId))
                        currentLabelAutomationId = childAutoId;
                }
                catch
                {
                    // If no child Text, use the CheckBox's own AutomationId or Name
                    if (!string.IsNullOrEmpty(checkBoxAutoId))
                        currentLabelAutomationId = checkBoxAutoId;
                }

                if (!string.IsNullOrEmpty(currentLabelAutomationId) && !toolMapping.ContainsKey(currentLabelAutomationId))
                {
                    var controls = new List<AppiumWebElement>();
                    if (statusImages.Count > 0)
                    {
                        controls.AddRange(statusImages);
                        statusImages = new List<AppiumWebElement>();
                    }
                    controls.Add(element);
                    toolMapping.Add(currentLabelAutomationId, controls);
                }
                continue;
            }

            // Text/TextBlock label — read AutomationId as the parameter name key.
            // Skip TextBlock elements (used for tab labels like "Parameters", "Environments").
            // Text elements with AutomationId are parameter labels.
            if (className == "Text" || className == "TextBlock")
            {
                // TextBlock elements are non-parameter labels (tabs, banners)
                if (className == "TextBlock")
                    continue;

                var automationId = GetAutomationId(element);

                // CUIT splits on '-' for list labels (e.g., "field_name-0")
                var paramName = automationId?.Split('-').FirstOrDefault();

                if (string.IsNullOrEmpty(paramName))
                    continue;

                // Skip if this Text is inside a CheckBox (already handled above)
                if (toolMapping.ContainsKey(paramName))
                    continue;

                currentLabelAutomationId = paramName;

                if (statusImages.Count > 0)
                {
                    toolMapping.Add(currentLabelAutomationId, new List<AppiumWebElement>(statusImages));
                    statusImages = new List<AppiumWebElement>();
                }
                else
                {
                    toolMapping.Add(currentLabelAutomationId, new List<AppiumWebElement>());
                }
                continue;
            }

            // All other elements (ComboBox, Button, PasswordBox, TextBox, etc.)
            // are added as controls for the current parameter.
            // Skip TextBox with AutomationId "PART_EditableTextBox" — these are
            // internal children of ComboBox, not standalone controls.
            if (!string.IsNullOrEmpty(currentLabelAutomationId) && toolMapping.ContainsKey(currentLabelAutomationId))
            {
                if (className == "TextBox" && GetAutomationId(element) == "PART_EditableTextBox")
                    continue;

                toolMapping[currentLabelAutomationId].Add(element);
            }
        }

        ToolControls = toolMapping;
        Trace.WriteLine($"[GpToolDialog] GetToolPaneControls found {toolMapping.Count} parameters: " +
                         string.Join(", ", toolMapping.Keys));
        return toolMapping;
    }

    /// <summary>
    /// Finds the first control of the given class name for a parameter.
    /// Builds the control map if not already cached.
    /// </summary>
    /// <param name="parameterName">The parameter name (AutomationId of the Text label).</param>
    /// <param name="className">The WPF class name to find (e.g., "ComboBox", "PasswordBox").</param>
    /// <param name="index">Zero-based index if multiple controls of the same class exist for the parameter.</param>
    /// <returns>The matching element, or <c>null</c> if not found.</returns>
    public AppiumWebElement? FindControlByClassName(string parameterName, string className, int index = 0)
    {
        EnsureToolControls();

        if (ToolControls == null || !ToolControls.ContainsKey(parameterName))
        {
            Trace.WriteLine($"[GpToolDialog] Parameter '{parameterName}' not found in tool controls. " +
                             $"Available: {string.Join(", ", ToolControls?.Keys ?? Enumerable.Empty<string>())}");
            return null;
        }

        var matches = ToolControls[parameterName]
            .Where(e => GetClassName(e) == className)
            .ToList();

        if (index >= matches.Count)
        {
            Trace.WriteLine($"[GpToolDialog] No {className} at index {index} for parameter '{parameterName}'. " +
                             $"Found {matches.Count} of that type.");
            return null;
        }

        return matches[index];
    }

    /// <summary>
    /// Sets a ComboBox parameter value by clicking the control and pasting text.
    /// Works for both feature layer dropdowns and free-text string inputs.
    /// </summary>
    /// <param name="parameterName">The parameter name (e.g., "in_facility_features", "servicenow_rest_url").</param>
    /// <param name="value">The value to set.</param>
    public void SetComboBoxValue(string parameterName, string value)
    {
        var comboBox = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                var control = FindControlByClassName(parameterName, "ComboBox");
                if (control == null) return null;
                if (IsOffscreen(control)) return null;
                return control;
            },
            timeoutMs: 15000,
            debugInfo: $"GpToolDialog.SetComboBoxValue — looking for ComboBox '{parameterName}'")
            ?? throw new InvalidOperationException(
                $"ComboBox not found for parameter '{parameterName}'.");

        comboBox.Click();
        WaitingUtils.Wait(300);

        // Use driver-level keyboard actions to type into the focused control.
        // Element-level SendKeys can fail on WPF ComboBox controls in WinAppDriver.
        // This mirrors CUIT's approach of ClickControlCenter → PasteValue.
        var actions = new Actions(_app.WinAppDriver);

        // Select all existing text, delete it, then type the new value
        actions.KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
            .SendKeys(Keys.Delete)
            .SendKeys(value)
            .SendKeys(Keys.Tab)
            .Perform();

        WaitingUtils.Wait(500);
    }

    /// <summary>
    /// Sets a PasswordBox parameter value (e.g., servicenow_password).
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="value">The password value.</param>
    public void SetPasswordValue(string parameterName, string value)
    {
        var passwordBox = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                var control = FindControlByClassName(parameterName, "PasswordBox");
                if (control == null) return null;
                if (IsOffscreen(control)) return null;
                return control;
            },
            timeoutMs: 15000,
            debugInfo: $"GpToolDialog.SetPasswordValue — looking for PasswordBox '{parameterName}'")
            ?? throw new InvalidOperationException(
                $"PasswordBox not found for parameter '{parameterName}'.");

        passwordBox.Click();
        WaitingUtils.Wait(300);

        var actions = new Actions(_app.WinAppDriver);
        actions.KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
            .SendKeys(Keys.Delete)
            .SendKeys(value)
            .SendKeys(Keys.Tab)
            .Perform();

        WaitingUtils.Wait(500);
    }

    /// <summary>
    /// Sets or clears a CheckBox parameter (e.g., keep_duplicate_value).
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="check"><c>true</c> to check, <c>false</c> to uncheck.</param>
    public void SetCheckBoxValue(string parameterName, bool check)
    {
        EnsureToolControls();

        if (ToolControls == null || !ToolControls.ContainsKey(parameterName))
            throw new InvalidOperationException($"Parameter '{parameterName}' not found.");

        // The CheckBox itself is stored as a control for the parameter
        var checkBox = ToolControls[parameterName]
            .FirstOrDefault(e => GetClassName(e) == "CheckBox")
            ?? throw new InvalidOperationException(
                $"CheckBox not found for parameter '{parameterName}'.");

        var isCurrentlyChecked = GetToggleState(checkBox);
        if (isCurrentlyChecked != check)
        {
            checkBox.Click();
            WaitingUtils.Wait(300);
        }
    }

    /// <summary>
    /// Gets the current text value of a ComboBox parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>The current value, or empty string if not found.</returns>
    public string GetComboBoxValue(string parameterName)
    {
        var comboBox = FindControlByClassName(parameterName, "ComboBox");
        if (comboBox == null) return string.Empty;

        try
        {
            // Try to get the value from the editable text within the ComboBox
            var textBox = comboBox.FindElementByClassName("TextBox");
            return textBox.Text ?? string.Empty;
        }
        catch
        {
            // Fallback: try the ComboBox's own text
            return comboBox.Text ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets the current checked state of a CheckBox parameter.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns><c>true</c> if checked.</returns>
    public bool GetCheckBoxValue(string parameterName)
    {
        EnsureToolControls();

        if (ToolControls == null || !ToolControls.ContainsKey(parameterName))
            return false;

        var checkBox = ToolControls[parameterName]
            .FirstOrDefault(e => GetClassName(e) == "CheckBox");

        return checkBox != null && GetToggleState(checkBox);
    }

    /// <summary>
    /// Returns <c>true</c> if the parameter exists in the tool's control map.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    public bool DoesParameterExist(string parameterName)
    {
        EnsureToolControls();
        return ToolControls?.ContainsKey(parameterName) ?? false;
    }

    /// <summary>
    /// Returns the list of all parameter names discovered in the tool dialog.
    /// </summary>
    public IReadOnlyList<string> GetParameterNames()
    {
        EnsureToolControls();
        return ToolControls?.Keys.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// Clicks the Run button to execute the tool.
    /// </summary>
    public void ClickRun()
    {
        var gpPane = ToolPane.FindElementByAccessibilityId("gp_doc_pane")
            ?? ToolPane;

        AppiumWebElement? runButton = null;

        // Try finding the run button from the GP doc pane first, then ToolPane, then MainWindow
        runButton = WaitingUtils.RetryAssignmentUntilSuccess(
            () =>
            {
                try { return gpPane.FindElementByAccessibilityId(RunButtonAutomationId); }
                catch { }
                try { return ToolPane.FindElementByAccessibilityId(RunButtonAutomationId); }
                catch { }
                try { return _app.MainWindow.FindElementByAccessibilityId(RunButtonAutomationId); }
                catch { }
                return null;
            },
            timeoutMs: 10000,
            debugInfo: "GpToolDialog.ClickRun — looking for run_btn");

        if (runButton == null)
            throw new InvalidOperationException("Run button not found in GP tool dialog.");

        runButton.Click();
    }

    /// <summary>
    /// Waits for the tool to finish execution by monitoring for the progress bar.
    /// </summary>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns><c>true</c> if the tool completed within the timeout.</returns>
    public bool WaitForToolCompletion(int timeoutMs = 120000)
    {
        // First wait for the progress bar to appear (tool started)
        WaitingUtils.Wait(2000);

        // Then wait for it to disappear (tool finished)
        return WaitingUtils.RetryUntilSuccessOrTimeout(
            () =>
            {
                try
                {
                    var progressBars = ToolPane.FindElementsByClassName("ProgressBar");
                    return !progressBars.Any(pb =>
                        !pb.GetAttribute("IsOffscreen").Equals("True", StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    return true;
                }
            },
            timeoutMs: timeoutMs,
            delayBetweenAttemptsMs: 2000);
    }

    #region Private helpers

    /// <summary>
    /// Ensures the tool controls map has been built.
    /// </summary>
    private void EnsureToolControls()
    {
        if (ToolControls == null)
            GetToolPaneControls();
    }

    /// <summary>
    /// Gets the ClassName attribute from an element, handling exceptions.
    /// </summary>
    private static string GetClassName(AppiumWebElement element)
    {
        try
        {
            return element.GetAttribute("ClassName") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the AutomationId attribute from an element, handling exceptions.
    /// </summary>
    private static string GetAutomationId(AppiumWebElement element)
    {
        try
        {
            return element.GetAttribute("AutomationId") ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if an element is offscreen.
    /// </summary>
    private static bool IsOffscreen(AppiumWebElement element)
    {
        try
        {
            return element.GetAttribute("IsOffscreen")
                ?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the toggle/checked state of a CheckBox element.
    /// </summary>
    private static bool GetToggleState(AppiumWebElement checkBox)
    {
        try
        {
            // WinAppDriver exposes the toggle pattern as a property
            var state = checkBox.GetAttribute("Toggle.ToggleState");
            return state == "1" || state?.Equals("On", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
