# ServiceNow Test Automation POC

WinAppDriver-based UI test harness for validating ArcGIS Pro + ServiceNow integration (**CDF** and **ILL**), built with C# / MSTest v2 / .NET 8. Architecture aligns with the Esri CUIT framework.

## Overview

This project automates ArcGIS Pro through [WinAppDriver](https://github.com/microsoft/WinAppDriver) to verify end-to-end ServiceNow integration behavior. Tests are written in **C# (MSTest v2)** and built with the .NET SDK, following patterns from the Esri CUIT (Coded UI Test) framework.

**What we test:**

- **CDF** — Custom Data Feed feature layers visible in ArcGIS Pro (query results, attributes, spatial accuracy)
- **ILL** — Indoor Location Loader geoprocessing tool execution in ArcGIS Pro (parameters, execution, results)
- **ArcGIS Pro UI workflows** — navigation, pane interaction, ribbon commands for CDF/ILL features

> **Cross-platform note:** Test code can be authored on any OS, but tests only run on Windows where WinAppDriver and ArcGIS Pro are available.

---

## Prerequisites

| Requirement | Version | Verify With |
|---|---|---|
| Windows | 10 or 11 | `echo %OS%` → `Windows_NT` |
| Developer Mode | Enabled | Settings → For Developers |
| .NET SDK | 8.0+ | `dotnet --version` |
| ArcGIS Pro | 3.x | `dir "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"` |
| WinAppDriver | 1.2.1 | `dir "C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"` |

---

## Getting Started (Windows)

Follow these steps **in order** the first time you set up the project on a Windows machine.

### Step 1 — Clone the repo

```cmd
git clone https://github.com/dylanwhite-velocity/sn-test-automation-poc.git
cd sn-test-automation-poc
```

If you already have the repo, pull the latest:

```cmd
git pull origin main
```

### Step 2 — Install the .NET 8 SDK

Download the latest .NET 8 SDK from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

Run the installer, then **open a new terminal** and verify:

```cmd
dotnet --version
```

Expected output:

```
8.0.xxx
```

If the version is lower than `8.0.100`, download the latest .NET 8 SDK.

### Step 3 — Enable Developer Mode

1. Open **Settings → System → For developers** (Windows 11) or **Settings → Update & Security → For developers** (Windows 10)
2. Toggle **Developer Mode** to **On**
3. Accept the confirmation dialog

This is required for WinAppDriver to function.

### Step 4 — Install WinAppDriver

1. Download `WindowsApplicationDriver_1.2.1.msi` from [WinAppDriver releases](https://github.com/Microsoft/WinAppDriver/releases)
2. Run the installer — accept the default path: `C:\Program Files (x86)\Windows Application Driver\`

> **Note:** You do **not** need to start WinAppDriver manually. Our test framework starts and stops it automatically in `TestEnvironment.AssemblyInitialize` / `AssemblyCleanup`.

### Step 5 — Verify ArcGIS Pro is installed

```cmd
dir "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"
```

If ArcGIS Pro is installed at a different path, note the path — you'll configure it in Step 7.

> **First launch:** If this is a fresh ArcGIS Pro install, launch it manually once, sign in, and close it. The test suite expects the main ArcGIS Pro window — not the sign-in dialog.

### Step 6 — Verify the full environment

Run all diagnostics to confirm everything is ready:

```cmd
echo %OS%
dotnet --version
dir "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"
dir "C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" /v AllowDevelopmentWithoutDevLicense
```

Expected results:

| Check | Expected Output |
|---|---|
| `echo %OS%` | `Windows_NT` |
| `dotnet --version` | `8.0.xxx` or higher |
| ArcGIS Pro dir | File listing (not "File Not Found") |
| WinAppDriver dir | File listing (not "File Not Found") |
| Developer Mode registry | `AllowDevelopmentWithoutDevLicense    REG_DWORD    0x1` |

If any check fails, revisit the corresponding step above.

### Step 7 — Restore packages and build

```cmd
dotnet restore
dotnet build
```

Both commands should complete successfully. If `dotnet build` fails:

- Check that the .NET SDK version is 8.0+ (`dotnet --version`)
- Run `dotnet restore` to download NuGet packages
- Check for specific error messages in the build output

### Step 8 — Run the smoke tests

```cmd
dotnet test --filter "TestCategory=Smoke" --settings test.runsettings --logger "console;verbosity=detailed"
```

You should see output like:

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
  Passed VerifyArcGisProLaunches [4m 15s]
  Passed VerifyMainWindowHasElements [3m 52s]
  Passed VerifyApplicationIsResponsive [3m 48s]

Test Run Successful.
Tests:    3 passed, 0 failed, 0 skipped
```

> **Startup time:** ArcGIS Pro takes 30–60+ seconds to launch per test. The framework waits 45 seconds by default. Override in `test.runsettings` via the `StartupWaitSeconds` parameter.

> **Sign-in dialog:** If a test fails because the window title doesn't contain "ArcGIS Pro", launch Pro manually, sign in, close it, then re-run.

---

## Running Tests

### Run all tests

```cmd
dotnet test --settings test.runsettings
```

### Run by category

```cmd
:: Smoke tests only
dotnet test --filter "TestCategory=Smoke" --settings test.runsettings

:: ILL tests only
dotnet test --filter "TestCategory=ILL" --settings test.runsettings

:: Discovery tests only
dotnet test --filter "TestCategory=Discovery" --settings test.runsettings

:: CDF tests only (future)
dotnet test --filter "TestCategory=CDF" --settings test.runsettings
```

### Run a specific test class or method

```cmd
:: All tests in a class
dotnet test --filter "FullyQualifiedName~ArcGisProLaunchTests" --settings test.runsettings

:: A single test method
dotnet test --filter "FullyQualifiedName~ArcGisProLaunchTests.VerifyArcGisProLaunches" --settings test.runsettings
```

### Detailed console output

```cmd
dotnet test --settings test.runsettings --logger "console;verbosity=detailed"
```

### Override runsettings parameters

You can override `test.runsettings` parameters from the command line:

```cmd
:: Custom ArcGIS Pro path
dotnet test --settings test.runsettings -- TestRunParameters.Parameter(name=\"ArcGISProPath\",value=\"D:\ArcGIS\Pro\bin\ArcGISPro.exe\")

:: Longer startup wait (default is 45s)
dotnet test --settings test.runsettings -- TestRunParameters.Parameter(name=\"StartupWaitSeconds\",value=\"90\")
```

Or edit `test.runsettings` directly:

```xml
<TestRunParameters>
  <Parameter name="ArcGISProPath" value="C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe" />
  <Parameter name="WinAppDriverUrl" value="http://127.0.0.1:4723" />
  <Parameter name="StartupWaitSeconds" value="45" />
</TestRunParameters>
```

---

## Test Inventory

**16 tests** across 4 categories. Latest run: **16/16 passed (100%)**.

### Smoke (3 tests) — `ArcGisProLaunchTests`

Validates the automation chain: WinAppDriver → ArcGIS Pro → UI element access.

| Test | Description |
|---|---|
| `VerifyArcGisProLaunches` | Pro launches via WinAppDriver, main window is accessible |
| `VerifyMainWindowHasElements` | UI element tree is accessible (ribbon, status bar, etc.) |
| `VerifyApplicationIsResponsive` | Pro responds to UI queries after launch |

### ILL (7 tests) — `IllToolboxCatalogTests`, `GpParameterLocationTests`

Validates ILL Python Toolbox visibility, GP tool dialog parameter access, and value set/get.

| Test | Description |
|---|---|
| `VerifyToolboxExistsInCatalog` | ILL toolbox visible under Catalog → Toolboxes |
| `VerifyIndoorsLocationLoaderOpensFromCatalog` | ILL tool opens in GP pane from Catalog |
| `VerifyGetToolPaneControlsFindsAllIllParameters` | GpToolDialog locates all 7 ILL parameters |
| `VerifySetComboBoxValueOnStringParameters` | Set/get ComboBox values (URL, username) |
| `VerifySetCheckBoxValueOnBooleanParameter` | Set/get CheckBox value (keep duplicates) |
| `VerifySetPasswordValueOnPasswordParameter` | Set PasswordBox value (ServiceNow password) |
| `VerifyGetParameterNamesReturnsExpectedList` | Parameter names match ILL `.pyt` definition |

### Discovery (6 tests) — `CatalogInspectionTests`, `GpParameterAccessibilityTests`

Infrastructure diagnostics — dump UI element trees and accessibility properties for POM development. Not regression tests.

| Test | Description |
|---|---|
| `DumpMainWindowTree` | Dumps ArcGIS Pro main window element tree |
| `DumpCatalogPaneTree` | Dumps Catalog pane tree (Toolboxes node) |
| `DumpRibbonTree` | Dumps ribbon element tree (View tab, buttons) |
| `DiagnoseCatalogToolNavigation` | Traces full Catalog → Toolbox → Tool navigation path |
| `DumpIllToolParameterAccessibility` | Dumps ILL GP parameter accessibility properties |
| `DumpBuiltinToolParameterAccessibility` | Dumps built-in GP tool (Buffer) properties for comparison |

### Running by category

```cmd
dotnet test --filter "TestCategory=Smoke" --settings test.runsettings
dotnet test --filter "TestCategory=ILL" --settings test.runsettings
dotnet test --filter "TestCategory=Discovery" --settings test.runsettings
```

> **Run time:** Full suite takes ~35 minutes. Each test launches and kills ArcGIS Pro independently (~2 min per test).

---

## Test Results

After running tests with `--settings test.runsettings`, results are auto-generated in `TestResults/`:

| File | Format | Purpose |
|---|---|---|
| `TestResults.trx` | XML (TRX) | CI/CD pipelines, Visual Studio import |
| `TestReport_<timestamp>.md` | Markdown | Human-readable — ✔️/❌ per test, durations, pass rate |
| `*_Failure.png` | PNG screenshot | Auto-captured on test failure by `ServiceNowTestClassBase` |

The Markdown report renders well in VS Code, GitHub PRs, or any Markdown viewer — useful for sharing with stakeholders.

---

## Project Structure

```
sn-test-automation-poc/
├── .editorconfig                                     # Code style (2-space indent, UTF-8)
├── .gitignore                                        # .NET build output, TestResults, etc.
├── AGENTS.md                                         # Full project conventions & architecture
├── README.md                                         # This file
├── sn-test-automation-poc.sln                        # Visual Studio solution
├── test.runsettings                                  # Test run configuration
│
└── src/
    ├── ServiceNow.TestHelpers/                       # Shared helper library
    │   ├── ServiceNow.TestHelpers.csproj             # net8.0 class library
    │   ├── Base/
    │   │   └── ServiceNowTestClassBase.cs            # Base test class (screenshots, logging)
    │   ├── ProApplication/
    │   │   ├── ActiProBase.cs                        # Root POM: WinAppDriver + MainWindow
    │   │   ├── Application.cs                        # ArcGIS Pro app wrapper
    │   │   ├── Ribbon/
    │   │   │   ├── RibbonTabBase.cs                  # Base for ribbon tabs
    │   │   │   ├── AnalysisTab.cs                    # Analysis tab → Geoprocessing
    │   │   │   ├── ViewTab.cs                        # View tab → Catalog pane
    │   │   │   ├── MapTab.cs                         # Map tab (stub)
    │   │   │   └── InsertTab.cs                      # Insert tab (stub)
    │   │   └── Pane/
    │   │       ├── PaneBase.cs                       # Base for dockable panes
    │   │       ├── CatalogPane.cs                    # Catalog pane (toolbox/folder tree)
    │   │       ├── ContentsPane.cs                   # Contents pane (layer list)
    │   │       ├── GeoprocessingPane.cs              # GP pane (tool search, run)
    │   │       └── GpToolDialog.cs                   # GP tool dialog (parameter map, set/get)
    │   └── Utilities/
    │       ├── ApplicationUtils.cs                   # Start/stop Pro, session mgmt
    │       ├── WinAppDriverUtils.cs                  # Start/stop WinAppDriver
    │       ├── WaitingUtils.cs                       # Retry-until-success patterns
    │       ├── ScreenCaptureUtils.cs                 # Screenshot capture
    │       └── UiTreeInspector.cs                    # Accessibility tree dump (element discovery)
    │
    └── ServiceNow.Integration.Tests/                 # Test project
        ├── ServiceNow.Integration.Tests.csproj       # MSTest test project
        ├── TestEnvironment.cs                        # AssemblyInitialize/Cleanup
        ├── ServiceNowTestBase.cs                     # Team test base (Pro lifecycle)
        ├── Smoke/
        │   └── ArcGisProLaunchTests.cs               # 3 smoke tests
        ├── Discovery/
        │   ├── CatalogInspectionTests.cs             # Catalog/ribbon element tree dumps
        │   ├── GpParameterAccessibilityTests.cs      # GP parameter accessibility analysis
        │   └── GpParameterLocationTests.cs           # GpToolDialog parameter map verification
        └── ILL/
            └── IllToolboxCatalogTests.cs             # ILL toolbox presence + tool discovery
```

### Key files

| File | Purpose |
|---|---|
| `ServiceNowTestClassBase.cs` | All tests inherit from this. Provides failure screenshots, trace logging, `[TestInitialize]`/`[TestCleanup]`. |
| `ServiceNowTestBase.cs` | Team-specific base. Starts/stops Pro per test via `StartProWithProject()`, disposes WinAppDriver sessions in cleanup. |
| `TestEnvironment.cs` | Runs once per assembly. Starts WinAppDriver in `[AssemblyInitialize]`, polls `/status` for readiness, stops it in `[AssemblyCleanup]`. |
| `ActiProBase.cs` | Root POM — finds the ArcGIS Pro main window from the WinAppDriver session. |
| `Application.cs` | Wraps the running Pro instance. Entry point for all POM access. |
| `CatalogPane.cs` | Catalog pane POM — tree navigation, toolbox expansion, tool discovery. |
| `GpToolDialog.cs` | GP tool dialog POM — parameter map, set/get values (ComboBox, PasswordBox, CheckBox), run tool. |
| `UiTreeInspector.cs` | Dumps accessibility tree for element discovery — used by Discovery tests. |
| `test.runsettings` | Configurable parameters: Pro path, WinAppDriver URL, startup wait, timeouts. |
| `AGENTS.md` | Comprehensive project conventions, architecture, coding standards, and CUIT alignment details. |

---

## Architecture

This project follows the [Esri CUIT framework](https://devtopia.esri.com/ArcGISPro/cuit/) patterns. See `AGENTS.md` for full architectural details.

### Test class hierarchy

```
ServiceNowTestClassBase                    (shared: screenshots, logging, lifecycle)
  └── ServiceNowTestBase                    (team: Pro start/stop per test)
        └── YourTestClass                   (your CDF/ILL test)
```

### Page Object Model (POM)

Tests **never** call `driver.FindElement*()` directly. All UI interaction goes through typed POM classes:

```csharp
// ✗ Wrong — raw element access in test code
var tab = driver.FindElementByAccessibilityId("nalysisTab");
tab.Click();

// ✓ Right — use POM classes
var analysisTab = new AnalysisTab(app);
analysisTab.EnableTab();
var gp = analysisTab.OpenGeoprocessing();
```

POM classes live in `ServiceNow.TestHelpers/ProApplication/` and encapsulate element IDs, waits, and retries.

---

## Writing New Tests

### 1. Decide if you need new POM classes

Check existing classes in `src/ServiceNow.TestHelpers/ProApplication/`. If the UI element you need isn't covered, you'll create a new POM class first.

### 2. Discover element identifiers (if new POM needed)

You need the element's **AutomationId**, **Name**, or **ClassName** from ArcGIS Pro.

1. Open **Inspect.exe** from the Windows SDK:
   ```cmd
   "C:\Program Files (x86)\Windows Kits\10\bin\x64\inspect.exe"
   ```
   > **Alternative:** [Accessibility Insights for Windows](https://accessibilityinsights.io/docs/windows/overview/) has a friendlier UI.
2. Launch ArcGIS Pro normally (not through WinAppDriver)
3. In Inspect.exe, hover over each target element in ArcGIS Pro
4. Note: **AutomationId**, **Name**, **ClassName**

Element location priority:

| Priority | Strategy | When to use |
|---|---|---|
| 1 | `FindElementByAccessibilityId()` | **Preferred** — most stable |
| 2 | `FindElementByName()` | When AutomationId unavailable |
| 3 | `FindElementByClassName()` | For generic element types |
| 4 | `FindElementByXPath()` | Last resort — fragile |

### 3. Create the POM class (if needed)

Add it to `src/ServiceNow.TestHelpers/ProApplication/` in the appropriate subfolder (Ribbon/, Pane/, Dialogs/, View/). Follow the existing patterns — constructor takes `Application`, use `WaitingUtils` for retries.

### 4. Create the test class

Create a new `.cs` file in the appropriate test subfolder (`CDF/` or `ILL/`). The directory will be created automatically when you add the file.

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.ProApplication;
using ServiceNow.TestHelpers.ProApplication.Pane;

namespace ServiceNow.Integration.Tests.CDF;

/// <summary>
/// Tests that CDF feature layers load correctly in ArcGIS Pro.
/// </summary>
[TestClass]
public class CdfFeatureLayerTests : ServiceNowTestBase
{
    /// <summary>
    /// Verifies that a CDF feature layer appears in the Contents pane.
    /// </summary>
    [TestMethod]
    [TestCategory("CDF")]
    [Description("Verify CDF layer loads in Contents pane")]
    public void VerifyLayerLoadsFromCdfService()
    {
        // Arrange
        var app = StartProWithProject(@"C:\TestData\CdfTestProject.aprx");
        var contentsPane = new ContentsPane(app);

        // Act
        bool layerExists = contentsPane.DoesLayerExist("ServiceNow Incidents");

        // Assert
        Assert.IsTrue(layerExists,
            "CDF feature layer 'ServiceNow Incidents' should appear in Contents pane");
    }
}
```

**Required attributes:** `[TestClass]`, `[TestMethod]`, `[TestCategory("CDF"|"ILL"|"Smoke")]`, `[Description]`

### 5. Build and run

```cmd
dotnet build
dotnet test --filter "FullyQualifiedName~CdfFeatureLayerTests" --settings test.runsettings --logger "console;verbosity=detailed"
```

### 6. Generate TestRail summary

After each new test, generate a TestRail-ready summary for the team to copy into [TestRail](https://testrail.esri.com). See `AGENTS.md` or the `servicenow-test-developer` agent for the template format.

---

## Development Workflow (Mac → Windows)

| Step | Machine | Command / Action |
|---|---|---|
| 1 | Mac | Author / edit test code in your IDE |
| 2 | Mac | `git add -A && git commit -m "feat(cdf): add layer test" && git push` |
| 3 | Windows VM | `git pull origin main` |
| 4 | Windows VM | `dotnet build` |
| 5 | Windows VM | `dotnet test --filter "TestCategory=Smoke" --settings test.runsettings` |
| 6 | Windows VM | Review results in `TestResults/` |

> WinAppDriver is started/stopped automatically — no need to launch it manually.

---

## Copilot Agents

This project includes custom Copilot agents for test development and execution:

- **ServiceNow Test Developer** — Generates WinAppDriver test cases following CUIT-aligned patterns, creates POM classes, verifies compilation
- **ServiceNow Test Runner** — Runs tests, diagnoses failures, reports results

Agent behavior and project conventions are defined in `AGENTS.md`.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| `dotnet` not recognized | .NET SDK not installed or not in PATH | Install from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download). If installed but not in PATH, use full path: `"C:\Program Files\dotnet\dotnet.exe"` |
| `dotnet build` fails with missing packages | NuGet restore not run | Run `dotnet restore` first |
| `dotnet build` fails with SDK version error | Wrong .NET SDK version | Install .NET 8 SDK |
| Connection refused on 4723 | WinAppDriver not installed | Install WinAppDriver ([releases](https://github.com/Microsoft/WinAppDriver/releases)) |
| "Developer Mode is not enabled" | Windows setting not toggled | Settings → For Developers → On |
| ArcGIS Pro not found | Wrong exe path | Edit `test.runsettings` → `ArcGISProPath` parameter |
| First test fails, rest pass | WinAppDriver cold-start timing | Already handled: `TestEnvironment` polls `/status` endpoint. If still failing, increase readiness timeout in `TestEnvironment.cs` |
| Test host process crashed | WinAppDriver session leak | Already handled: `ServiceNowTestBase` calls `Driver.Quit()` in cleanup. If still crashing, check that all sessions are disposed |
| Window title doesn't contain "ArcGIS Pro" | Sign-in/licensing dialog on first launch | Sign in to Pro manually once, close it, re-run tests |
| Test timeout | ArcGIS Pro startup too slow | Increase `StartupWaitSeconds` in `test.runsettings`. Discovery tests may need `[Timeout(600000)]` |
| "Could not find ArcGIS Pro main window" | Pro crashed or didn't start | Check Pro install, verify exe path, increase startup wait |
| Catalog tree expansion fails | .pyt toolbox parsing delay | POM uses click + ArrowRight with 3s wait; increase wait in `CatalogPane.cs` if needed |
| GP tool search returns wrong results | Keyboard navigation timing | POM retries search up to 2 times with verification; increase retries in `GeoprocessingPane.cs` if needed |

---

## Next Steps

- [ ] **First CDF test** — Create a test `.aprx` project with a CDF feature service, write `CdfFeatureLayerTests` in `CDF/`
- [ ] **ILL execution test** — Run the ILL tool end-to-end (set parameters, click Run, verify output)
- [ ] **Test data strategy** — Define `.aprx` project files and test data for repeatable CDF/ILL tests
- [ ] **CI pipeline** — GitHub Actions on a Windows runner for automated test execution on PR
- [ ] **TestRail automation** — API integration to auto-sync test cases and report results from TRX files

---

## Related Resources

- [WinAppDriver Documentation](https://github.com/microsoft/WinAppDriver/tree/master/Docs)
- [WinAppDriver Samples (C#)](https://github.com/microsoft/WinAppDriver/tree/master/Samples/C%23)
- [Appium.WebDriver NuGet](https://www.nuget.org/packages/Appium.WebDriver/)
- [MSTest v2 Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)
- [Inspect.exe (UI element inspector)](https://learn.microsoft.com/en-us/windows/win32/winauto/inspect-objects)
- [Accessibility Insights for Windows](https://accessibilityinsights.io/docs/windows/overview/)
- [ArcGIS Enterprise CDF Documentation](https://developers.arcgis.com/enterprise-sdk/guide/custom-data-feeds/)
- [ArcGIS Pro Python Toolbox](https://pro.arcgis.com/en/pro-app/latest/arcpy/geoprocessing_and_python/a-quick-tour-of-creating-tools-in-python.htm)
- [Esri CUIT Repo (internal)](https://devtopia.esri.com/ArcGISPro/cuit/)


