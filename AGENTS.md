# AGENTS.md — ServiceNow Test Automation POC

> Companion test harness for the [ServiceNow-ArcGIS Integration](https://github.com/EsriPS/ServiceNow_Esri_Integration) monorepo. This project validates the **CDF** (Custom Data Feed) and **ILL** (Indoor Location Loader) integrations through automated UI tests driven by WinAppDriver against ArcGIS Pro.
>
> **Architecture aligns with the Esri CUIT repo** — the canonical ArcGIS Pro UI testing framework used by all Esri product teams. We follow the same technology stack (C# / MSTest v2 / .NET), Page Object Model patterns, and test hierarchy conventions.

## Project Overview

This is a **WinAppDriver UI test harness** that automates ArcGIS Pro to verify ServiceNow integration behavior end-to-end. Tests are written in **C# (MSTest v2)** and built with the .NET SDK, following patterns established by the Esri CUIT framework.

The goal of the broader ServiceNow-ArcGIS integration is to surface ServiceNow operational data — incidents, work orders, assets, facilities — on interactive, spatially-aware maps within ArcGIS. ServiceNow records are extracted via the ServiceNow REST API, then transformed by a Custom Data Feed (CDF) service running in ArcGIS Server into an ArcGIS-compatible Feature Service. Records are streamed as dynamic features and aligned to indoor floor plans using building, level, and space identifiers. This test harness validates that integration from the ArcGIS Pro client perspective.

**What we test:**
- **CDF integration** — Custom Data Feed feature layers visible in ArcGIS Pro maps (query results, attribute fidelity, spatial accuracy)
- **ILL integration** — Indoor Location Loader geoprocessing tool execution within ArcGIS Pro (parameter validation, layer output, ServiceNow write-back verification)
- **ArcGIS Pro UI workflows** — navigation, pane interaction, ribbon commands as they relate to CDF/ILL features

**What we do NOT test:**
- ServiceNow REST API directly (covered by CDF unit tests in the integration repo)
- ILL Python logic directly (covered by `ill/tests/` in the integration repo)
- UIB (separate team, out of scope)

### Integration Goals

- Provide read-only feature access from live ServiceNow data into ArcGIS clients (Pro, web maps, dashboards)
- Expose ServiceNow business objects (Asset, Configuration Item, Task and their subclasses) as point-geometry feature layers using location coordinates from `cmn_location`
- Support an incremental ArcGIS `/query` capability set (attribute filters, field projection, paging, spatial envelope intersects)
- Deliver a deployable `.cdpk` provider package for ArcGIS Enterprise 11.1+

### Integration Non-Goals

- Editing / write-back to ServiceNow (create, update, delete)
- Full ArcGIS `/query` surface area (statistics, distinct, groupBy, advanced spatial relations)
- Polygon or line geometry support
- API Key for ServiceNow auth (MVP uses basic auth)

### Key Technology References

- [ServiceNow REST API](https://www.servicenow.com/docs/r/api-reference/api-implementation-reference.html)
- [ArcGIS Enterprise Custom Data Feeds](https://developers.arcgis.com/enterprise-sdk/guide/custom-data-feeds/)
- [ArcGIS Pro Custom Geoprocessing Tool](https://pro.arcgis.com/en/pro-app/latest/help/analysis/geoprocessing/basics/use-a-custom-geoprocessing-tool.htm)
- [Floor Aware Maps](https://pro.arcgis.com/en/pro-app/latest/help/data/indoors/floor-aware-maps.htm)
- [Indoors Viewer](https://doc.arcgis.com/en/indoors/latest/viewer/introduction-to-indoor-viewer.htm)

### Relationship to Other Repos

| Repo | Purpose |
|---|---|
| [`EsriPS/ServiceNow_Esri_Integration`](https://github.com/EsriPS/ServiceNow_Esri_Integration) | Source code for CDF (JS) and ILL (Python). Unit tests, linting, documentation. |
| [`sn-test-automation-poc`](https://github.com/dylanwhite-velocity/sn-test-automation-poc) | **This repo.** End-to-end UI tests validating CDF/ILL through ArcGIS Pro. |
| CUIT (`../cuit`) | Canonical ArcGIS Pro UI test framework. Cloned as a sibling directory. **Always check here first before writing new code.** Key paths: `UITestingHelpers/ProApplication/` (POM classes), `UITestingHelpers/Utilities/` (utilities), `ArcGISGeoProcessingUITests/` (GP patterns), `ArcGISIndoorsUITests/` (indoor tests). |

When CDF or ILL behavior changes in the integration repo, corresponding test cases here may need updates. Cross-reference PRs by linking to the integration repo issue number.

---

## CUIT-Aligned Architecture

Our test architecture mirrors the patterns from the Esri CUIT framework. Understanding CUIT is essential for writing tests correctly.

### Key CUIT Concepts

| CUIT Component | Our Equivalent | Purpose |
|---|---|---|
| `UITestingHelpers.UITestClassBase` | `ServiceNow.TestHelpers.ServiceNowTestClassBase` | Base test class with `[TestInitialize]`/`[TestCleanup]`, failure screenshots, trace logging |
| `UITestingHelpers.ProApplication.Application` | `ServiceNow.TestHelpers.ProApplication.Application` | Page Object wrapping the running ArcGIS Pro instance |
| `UITestingHelpers.Controls.Base.ActiProBase` | `ServiceNow.TestHelpers.ProApplication.ActiProBase` | Root POM base holding `WinAppDriver` driver and `MainWindow` |
| `UITestingHelpers.Utilities.ApplicationUtils` | `ServiceNow.TestHelpers.Utilities.ApplicationUtils` | Start/stop Pro via WinAppDriver, get desktop sessions, kill processes |
| `UITestingHelpers.Utilities.WinAppDriverUtilities` | `ServiceNow.TestHelpers.Utilities.WinAppDriverUtils` | Start/stop WinAppDriver programmatically |
| Team `TestEnvironment` class | `ServiceNow.Integration.Tests.TestEnvironment` | `[AssemblyInitialize]`/`[AssemblyCleanup]` — starts WAD, configures registry |
| Team `TestBase` class | `ServiceNow.Integration.Tests.ServiceNowTestBase` | Team-specific base extending `ServiceNowTestClassBase` |
| `[VideoLoggedTestClass]` | `[TestClass]` (video TBD) | Class-level test attribute |
| `[AppDriverOptions]` | `[AppDriverOptions]` | Select WAD v1 or v2 driver |

### Test Class Hierarchy

```
ServiceNowTestClassBase                         (shared: session lifecycle, screenshots, logging)
  └── ServiceNowTestBase                         (team: start/kill Pro per test, crash dump monitoring)
        ├── CdfFeatureLayerTests                 (CDF tests)
        ├── CdfAttributeTests                    (CDF tests)
        ├── IllToolExecutionTests                (ILL tests)
        └── ...
```

### Page Object Model (POM)

Tests **never** call `driver.FindElementByAccessibilityId(...)` directly. Instead, they interact through typed POM classes:

```csharp
// WRONG — raw element access in test code
var tab = driver.FindElementByAccessibilityId("nalysisTab");
tab.Click();

// RIGHT — use POM classes
var analysisTab = new AnalysisTab(Application);
analysisTab.EnableTab();
var gp = analysisTab.OpenGeoprocessing();
```

POM classes live in the `ServiceNow.TestHelpers` project and encapsulate all element location logic, waits, and retries.

### Assembly Lifecycle (TestEnvironment)

Every test project has a `TestEnvironment` class that runs once per assembly:

```csharp
[TestClass]
public class TestEnvironment
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        WinAppDriverUtils.StartWinAppDriver();
        // Additional one-time setup
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        WinAppDriverUtils.CloseWinAppDriver();
    }
}
```

WinAppDriver is started/stopped **programmatically** — you do not need to manually launch it before running tests.

### WinAppDriver Splash Screen Workaround

> **Critical:** ArcGIS Pro displays a splash screen on launch. WinAppDriver attaches its initial session to the splash screen window, which closes before the main window appears — orphaning the session. All subsequent `FindElement` calls return 404.

**Solution (implemented in `ServiceNowTestBase.StartProWithProject()`):**

1. Launch ArcGIS Pro via `ApplicationUtils.StartApplicationWAD()` — this gets a session, but it may be scoped to the splash screen
2. Wait for Pro to initialize (`StartupWaitSeconds`)
3. **Discard** the launch session (`launchSession.Quit()`)
4. **Re-attach** via `ApplicationUtils.GetExistingDesktopSession()` — this creates a root desktop session, finds the actual `ArcGISProMainWindow` by AutomationId, and creates a new session attached to its window handle

```csharp
// Launch Pro (session may be orphaned after splash screen closes)
var launchSession = ApplicationUtils.StartApplicationWAD(...);
WaitingUtils.Wait(StartupWaitSeconds * 1000);

// Discard orphaned session, re-attach via desktop
try { launchSession.Quit(); } catch { }
Driver = ApplicationUtils.GetExistingDesktopSession(WinAppDriverUrl);
Application = new Application(Driver);
```

This pattern ensures a stable WinAppDriver session regardless of Pro's splash screen behavior.

---

## Repository Structure

```
sn-test-automation-poc/
├── .editorconfig                                     # Code style (2-space indent, UTF-8)
├── AGENTS.md                                         # This file
├── README.md                                         # Setup guide, troubleshooting
├── .gitignore
├── sn-test-automation-poc.sln                        # Solution file
├── test.runsettings                                  # Test run configuration
│
├── src/
│   ├── ServiceNow.TestHelpers/                       # Shared helper library (our UITestingHelpers)
│   │   ├── ServiceNow.TestHelpers.csproj
│   │   ├── Base/
│   │   │   └── ServiceNowTestClassBase.cs            # Base test class (like UITestClassBase)
│   │   ├── ProApplication/
│   │   │   ├── ActiProBase.cs                        # Root POM base: WinAppDriver + MainWindow
│   │   │   ├── Application.cs                        # ArcGIS Pro application wrapper
│   │   │   ├── Ribbon/
│   │   │   │   ├── RibbonTabBase.cs                  # Base for all ribbon tabs
│   │   │   │   ├── AnalysisTab.cs                    # Analysis ribbon tab
│   │   │   │   ├── MapTab.cs                         # Map ribbon tab (stub)
│   │   │   │   └── InsertTab.cs                      # Insert ribbon tab (stub)
│   │   │   └── Pane/
│   │   │       ├── PaneBase.cs                       # Base for all dockable panes
│   │   │       ├── ContentsPane.cs                   # Contents pane (layer list)
│   │   │       └── GeoprocessingPane.cs              # Geoprocessing pane (for ILL)
│   │   └── Utilities/
│   │       ├── ApplicationUtils.cs                   # Start/stop Pro, session management
│   │       ├── WinAppDriverUtils.cs                  # Start/stop WinAppDriver
│   │       ├── WaitingUtils.cs                       # Retry-until-success patterns
│   │       └── ScreenCaptureUtils.cs                 # Screenshot capture
│   │
│   └── ServiceNow.Integration.Tests/                 # Test project
│       ├── ServiceNow.Integration.Tests.csproj
│       ├── TestEnvironment.cs                        # [AssemblyInitialize/Cleanup]
│       ├── ServiceNowTestBase.cs                     # Team-specific test base
│       └── Smoke/
│           └── ArcGisProLaunchTests.cs               # Smoke tests (3 tests)
```

> **Note:** `CDF/`, `ILL/`, `Dialogs/`, and `View/` directories will be created as tests
> and POM classes are added. They do not exist yet in the repo.

### Key directories

| Path | Purpose |
|---|---|
| `src/ServiceNow.TestHelpers/` | Shared helper library — POM classes, utilities, base classes |
| `src/ServiceNow.TestHelpers/ProApplication/` | Page Object Model for ArcGIS Pro UI elements |
| `src/ServiceNow.TestHelpers/Utilities/` | Shared utilities (wait/retry, screenshots, app lifecycle) |
| `src/ServiceNow.Integration.Tests/` | Test classes organized by integration area |
| `src/ServiceNow.Integration.Tests/Smoke/` | Smoke tests validating the automation chain |
| `TestResults/` | MSTest output — TRX files, logs, screenshots (gitignored) |

---

## Technology Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET 8+) |
| Test framework | MSTest v2 (`Microsoft.VisualStudio.TestTools.UnitTesting`) |
| UI automation | WinAppDriver via Appium `WindowsDriver<AppiumWebElement>` |
| Appium client | `Appium.WebDriver` NuGet package |
| Build tool | .NET SDK (`dotnet build`, `dotnet test`) |
| Target application | ArcGIS Pro 3.x (WPF desktop app, Windows only) |
| Integration source | ServiceNow REST API → CDF Feature Service / ILL Python Toolbox |

---

## Build & Test Commands

All test execution requires **Windows** with ArcGIS Pro installed. WinAppDriver is started programmatically by `TestEnvironment.AssemblyInitialize`.

**Always use `--settings test.runsettings`** — this ensures standardized output: a TRX report, a Markdown report, and console output are generated automatically on every run.

```cmd
:: Restore NuGet packages
dotnet restore

:: Build (verify compilation, no test execution)
dotnet build

:: Run all tests (reports auto-generated in TestResults/)
dotnet test --settings test.runsettings

:: Run only CDF tests (by filter)
dotnet test --filter "TestCategory=CDF" --settings test.runsettings

:: Run only ILL tests (by filter)
dotnet test --filter "TestCategory=ILL" --settings test.runsettings

:: Run a specific test class
dotnet test --filter "FullyQualifiedName~CdfFeatureLayerTests" --settings test.runsettings

:: Run a specific test method
dotnet test --filter "FullyQualifiedName~CdfFeatureLayerTests.VerifyLayerLoadsFromCdfService" --settings test.runsettings
```

### Test Reports (Automatic)

Every test run with `--settings test.runsettings` produces these artifacts in `TestResults/`:

| File | Format | Audience |
|---|---|---|
| `TestResults.trx` | XML (TRX) | CI/CD pipelines, Visual Studio import |
| `TestReport_<timestamp>.md` | Markdown | Managers, stakeholders, GitHub PRs — renders with ✔️/❌ pass/fail, durations, pass rate |

The Markdown report includes:
- Overall pass/fail status with pass rate percentage
- Run duration and date range
- Per-test results with ✔️/❌ status and duration
- Error messages for any failures

To share with non-technical stakeholders: open the `.md` file in VS Code preview, paste into a GitHub issue/PR, or use any Markdown viewer.

### Test Run Settings (test.runsettings)

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>1</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TargetPlatform>x64</TargetPlatform>
    <TestSessionTimeout>36000000</TestSessionTimeout>
  </RunConfiguration>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="trx">
        <Configuration>
          <LogFileName>TestResults.trx</LogFileName>
        </Configuration>
      </Logger>
      <Logger friendlyName="liquid.md" />
      <Logger friendlyName="console">
        <Configuration>
          <Verbosity>normal</Verbosity>
        </Configuration>
      </Logger>
    </Loggers>
  </LoggerRunSettings>
  <MSTest>
    <CaptureTraceOutput>True</CaptureTraceOutput>
    <DeploymentEnabled>True</DeploymentEnabled>
    <DeleteDeploymentDirectoryAfterTestRunIsComplete>False</DeleteDeploymentDirectoryAfterTestRunIsComplete>
    <TestTimeout>450000</TestTimeout>
  </MSTest>
  <TestRunParameters>
    <Parameter name="ArcGISProPath" value="C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe" />
    <Parameter name="WinAppDriverUrl" value="http://127.0.0.1:4723" />
    <Parameter name="StartupWaitSeconds" value="45" />
  </TestRunParameters>
</RunSettings>
```

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Windows | 10/11 | WinAppDriver is Windows-only |
| Developer Mode | Enabled | Required for WinAppDriver |
| .NET SDK | 8.0+ | `dotnet --version` to verify |
| WinAppDriver | 1.2.1 | Started programmatically by tests; must be installed |
| ArcGIS Pro | 3.x | Must be licensed. Single Use License (SUL) or Named User License. If using Named User, sign in at least once manually before running tests. |
| Visual Studio | 2022+ (optional) | For IDE-based test running; `dotnet test` works from CLI |

---

## Branching Strategy

```
main                              # Stable, reviewed tests
└── feature/<story-id>-desc       # Feature branches per story/task
```

PR titles follow: `(feat|fix|cicd|chore)/<issue-number>-short-description`

## Issue & PR Conventions

### PR Template
- Title: `(feat|fix|cicd|chore)/<issue-number>-short-description`
- Must include: description, what CDF/ILL behavior is being tested, proof of test execution
- Cross-reference integration repo issues when applicable: `Related: EsriPS/ServiceNow_Esri_Integration#<number>`

### Git Commit Message Format
Follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) with an optional scope for the area of the project being changed. Scopes help identify which part of the project a commit relates to:
- `cdf` — CDF-related test code
- `ill` — ILL-related test code
- `base` — shared base classes or TestHelpers
- `pom` — Page Object Model classes
- `deps` — dependency updates
- `docs` — documentation changes
- `all` — changes spanning multiple areas

Examples:
- `feat(cdf): add feature layer visibility test`
- `fix(base): increase implicit wait for slow environments`
- `chore(deps): update Appium.WebDriver NuGet package`
- `docs(all): update AGENTS.md with integration goals`

---

## Coding Standards

### Shared Standards (All Code)

These rules apply to **every file in the repository**, regardless of language. Adapted from the [integration repo coding standards](https://github.com/EsriPS/ServiceNow_Esri_Integration).

#### MUST have

- Git **must** be used for version control and code management
- All code **must** use meaningful variable and function names (e.g., `featureLayerName` not `fln`, `verifyLayerLoadsFromCdfService` not `test1`)
- All tests **must** pass before merging
- All code **must** be reviewed by at least one other developer before merging
- When providing credentials or passwords, **must** use the placeholder "ask a ServiceNow Integration Team member" instead of actual values
- Each section of related code **must** be documented
- Each public class/method and their parameters **must** have documentation comments (`/// <summary>` in C#)

#### SHOULD have

- Code **should** avoid using global variables whenever possible
- Exception handling **should** be used to gracefully handle errors, especially test setup/teardown failures
- Dependencies **should** be updated regularly to ensure security and compatibility
- The following principles **should** be followed:
  - **Single Responsibility** — each class/method does one thing well
  - **DRY** (Don't Repeat Yourself) — extract shared logic into helpers/POM classes
  - **KISS** (Keep It Simple Stupid) — prefer clarity over cleverness
  - **YAGNI** (You Aren't Gonna Need It) — don't build what isn't needed yet
  - **DMMT** (Don't Make Me Think) — code should be self-explanatory
  - **KIM** (Keep It Maintainable) — write for the next developer, not just today
  - **APO** (Avoid Premature Optimization) — correctness before performance
  - **SOC** (Separation of Concerns) — POM classes vs test logic vs utilities

#### COULD have

- Complicated or hard-to-read code **could** have inline comments, however the code should be refactored to be more readable if possible

#### WON'T have

- Code **won't** have unnecessary code duplication
- Code **won't** have unnecessary comments which add nothing to the code

### C# / MSTest Standards

- Target **.NET 8+** with C# 12+ language features
- All test classes **must** use `[TestClass]` attribute
- All test methods **must** use `[TestMethod]` attribute
- All test methods **must** have `[TestCategory]` attribute (`"CDF"`, `"ILL"`, or `"Smoke"`)
- All test methods **must** have `[Description]` attribute linking to the issue being validated
- Test classes **must** extend `ServiceNowTestBase` (which extends `ServiceNowTestClassBase`)
- Test classes **must** have a `TestContext` property:
  ```csharp
  public TestContext TestContext { get; set; }
  ```
- Use `Assert.IsTrue`, `Assert.AreEqual`, `Assert.IsNotNull`, etc. with descriptive messages
- Use **POM classes** for all UI interactions — never call `FindElement*` directly in test methods
- Use `/// <summary>` XML doc comments on public classes and methods

### Page Object Model (POM) Standards

- **Check CUIT first** — before creating a new POM class, check `../cuit/UITestingHelpers/UITestingHelpers/ProApplication/` for an existing implementation. If it exists, **copy the CUIT file into our repo and edit it** to fit our needs (adjust namespaces, trim unused methods, adapt for ServiceNow-specific behavior). Only write from scratch when no CUIT equivalent exists.
- POM classes live in `ServiceNow.TestHelpers/ProApplication/`
- Every POM class receives an `Application` instance (or its parent POM) via constructor
- POM classes encapsulate element location, waits, and retries internally
- POM methods return typed results (other POM objects, strings, booleans) — not raw `AppiumWebElement`
- Use `WaitingUtils.RetryUntilSuccessOrTimeout()` for element waits, not `Thread.Sleep()`
- Follow CUIT naming: `AnalysisTab`, `ContentsPane`, `GeoprocessingPane`, `MapView`

### Element Location Strategy

When locating ArcGIS Pro UI elements inside POM classes, use this priority order:

| Priority | Strategy | Example | When to use |
|---|---|---|---|
| 1 | AccessibilityId | `element.FindElementByAccessibilityId("nalysisTab")` | **Preferred** — most stable across Pro versions |
| 2 | Name | `element.FindElementByName("Analysis")` | When AutomationId is unavailable |
| 3 | ClassName | `element.FindElementByClassName("RibbonTabHeader")` | For generic element types |
| 4 | XPath | `element.FindElementByXPath("//Button[@Name='OK']")` | Last resort — fragile across versions |

Use **Inspect.exe** (`C:\Program Files (x86)\Windows Kits\10\bin\x64\inspect.exe`) or **Accessibility Insights for Windows** to discover element identifiers.

### Test Class Template

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceNow.TestHelpers.ProApplication;
using ServiceNow.TestHelpers.ProApplication.Ribbon;
using ServiceNow.TestHelpers.Utilities;

namespace ServiceNow.Integration.Tests.CDF
{
    /// <summary>
    /// Tests that verify CDF feature layers load correctly in ArcGIS Pro.
    /// Validates: layer visibility, attribute presence, spatial accuracy.
    /// </summary>
    [TestClass]
    public class CdfFeatureLayerTests : ServiceNowTestBase
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Verifies that a CDF feature layer loads and is visible in the Contents pane.
        /// Related: EsriPS/ServiceNow_Esri_Integration#123
        /// </summary>
        [TestMethod]
        [TestCategory("CDF")]
        [Description("https://github.com/EsriPS/ServiceNow_Esri_Integration/issues/123")]
        public void VerifyLayerLoadsFromCdfService()
        {
            // Arrange — open a project with the CDF feature service
            var app = StartProWithProject("CdfTestProject.aprx");
            var contentsPane = new ContentsPane(app);

            // Act — check layer presence
            bool layerExists = contentsPane.DoesLayerExist("ServiceNow Incidents");

            // Assert
            Assert.IsTrue(layerExists, "CDF feature layer 'ServiceNow Incidents' should be visible in Contents pane");
        }
    }
}
```

---

## Integration Context

### CDF (Custom Data Feed)

The CDF is a JavaScript provider deployed as a `.cdpk` package on ArcGIS Enterprise 11.1+. It translates ServiceNow table data (incidents, work orders, assets, facilities) into an ArcGIS Feature Service with point geometry. Tests in this repo verify CDF behavior as seen from ArcGIS Pro — feature layers loading, attribute values matching ServiceNow records, spatial placement.

**Source:** `cdf/` in [`EsriPS/ServiceNow_Esri_Integration`](https://github.com/EsriPS/ServiceNow_Esri_Integration) (branch: `dev-cdf`)

### ILL (Indoor Location Loader)

The ILL is an ArcGIS Pro Python Toolbox (`.pyt`) that loads indoor location data from ArcGIS feature layers into ServiceNow `cmn_location` records. It runs inside ArcGIS Pro's bundled Python environment. Tests in this repo verify ILL behavior through the ArcGIS Pro geoprocessing UI — parameter input, tool execution, result validation.

**Source:** `ill/` in [`EsriPS/ServiceNow_Esri_Integration`](https://github.com/EsriPS/ServiceNow_Esri_Integration) (branch: `dev-ill`)

### How CUIT Tests GP Tools (ILL Reference)

The CUIT Geoprocessing tests provide the pattern for our ILL tests:

```csharp
// CUIT pattern: Open a project, navigate to Analysis tab, open GP tool
var app = StartProInWinAppDriver(projectPath);
var analysisTab = new AnalysisTab(app);
analysisTab.EnableTab();

// Open the Geoprocessing pane and search for a tool
var gp = new Geoprocessing(app);
gp.SearchForTool("Indoor Location Loader");

// Set parameters via the tool dialog
gp.ToolDialogPage.SetParameterValue("Input Features", inputLayer);
gp.ToolDialogPage.ClickRun();

// Wait for execution and verify results
Assert.IsTrue(gp.DidToolSucceed(), "ILL tool should complete successfully");
```

---

## Agent Roles

### Shared Protocol: Pause for Blockers

All agents **must** follow this protocol before proceeding with any task that depends on external information:

1. **Gather** — Read all available inputs (issues, PRs, source code, existing tests, CUIT reference patterns).
2. **Assess** — Determine whether you have enough information to produce an accurate, complete result.
3. **Pause if blocked** — If any required information is missing, ambiguous, or contradictory:
   - **Stop work** immediately. Do not guess or fill in gaps with assumptions.
   - Surface **specific, numbered questions** to the user. Each question should explain what is missing and why it blocks progress.
   - **Wait** for the user to respond before continuing.
4. **Resume** — Once the user answers, incorporate the answers and proceed.

### Test Developer

**Trigger:** Asked to write a new test case, extend coverage, or create test scaffolding for a CDF/ILL feature.

**Workflow:**
1. **Check the CUIT repo first** — before writing any new POM class, utility, or test pattern, search the sibling `../cuit` repo for existing implementations. Key locations: `UITestingHelpers/ProApplication/Pane/` (panes), `UITestingHelpers/ProApplication/Ribbon/` (ribbon tabs), `UITestingHelpers/Utilities/` (utilities), `UITestingHelpers/Controls/Extensions/` (control helpers). If the functionality already exists in CUIT, **copy the file into our repo and edit it as necessary** — adjust namespaces, remove unused methods, adapt for ServiceNow-specific needs. Only write from scratch when no CUIT equivalent exists.
2. Read the existing test base classes and POM classes to understand what's already available
3. Identify which CDF/ILL feature is being tested and locate the relevant source in the integration repo
4. Determine which ArcGIS Pro UI elements are involved — check existing POM classes first, then Inspect.exe
5. If new POM classes are needed, create them in `ServiceNow.TestHelpers/ProApplication/` following CUIT patterns
6. Create the test class in the appropriate subdirectory (`CDF/` or `ILL/`)
7. Ensure the test extends `ServiceNowTestBase` and uses `[TestMethod]`, `[TestCategory]`, `[Description]`
8. Use POM classes for all UI interaction — never raw `FindElement*` in test methods
9. Verify the test compiles with `dotnet build`

**Rules:**
- Every test must have XML doc comments explaining what integration behavior it validates
- Tests must be deterministic — no reliance on transient ServiceNow data without setup/teardown
- Use `Assert` messages that explain what was expected vs what was found
- Failure screenshots are captured automatically by `ServiceNowTestClassBase`
- Reference the integration repo issue number in `[Description]` attribute
- New POM classes must follow the CUIT pattern: constructor takes `Application`, element access via `FindElementByAccessibilityId` with retry/wait wrappers

### Test Runner

**Trigger:** Asked to run tests, verify results, or diagnose failures.

**Workflow:**
1. Verify the Windows environment (ArcGIS Pro installed, Developer Mode enabled, WinAppDriver installed)
2. Run `dotnet test --settings test.runsettings` (with `--filter` for a subset) — **always include `--settings test.runsettings`** to get standardized reports
3. Report results: passed/failed count, failure details, and note the report files in `TestResults/` (TRX + Markdown)
4. If tests fail, examine the failure output, Markdown report, screenshots, and relevant source to diagnose the root cause
5. Distinguish between test bugs (our code), POM bugs (element IDs changed), and integration bugs (CDF/ILL behavior changed)

**Rules:**
- **Always use `--settings test.runsettings`** when running tests — this ensures TRX and Markdown reports are auto-generated
- Never modify test files or source code unless explicitly asked
- Report the exact error output — do not summarize away details
- For environment issues (WinAppDriver not installed, Pro not found), diagnose and provide the fix
- Note when failures may indicate a regression in the integration repo's CDF or ILL code

---

## Team

| Role | Name |
|------|------|
| Product Owner | Nick Jones |
| Lead Dev | Tom Hahka |
| Dev | Qing Ying Wu |
| Dev | Yannis Maroufidis |
| SM/PE | Palak Matta |
| PE | Dylan White |
| UIB POC | Gary Knoll |

---

## Use of AI in Code

- Esri approved AI tools and usage guidelines
- AI-generated code must meet same standards as human-written code
- All (every line of) AI-generated code must be read, understood, and tested by the person employing it — responsibility stays with them
