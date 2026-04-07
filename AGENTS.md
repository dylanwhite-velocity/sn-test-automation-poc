# AGENTS.md — ServiceNow Test Automation POC

> Companion test harness for the [ServiceNow-ArcGIS Integration](https://github.com/EsriPS/ServiceNow_Esri_Integration) monorepo. This project validates the **CDF** (Custom Data Feed) and **ILL** (Indoor Location Loader) integrations through automated UI tests driven by WinAppDriver against ArcGIS Pro.

## Project Overview

This is a **WinAppDriver UI test harness** that automates ArcGIS Pro to verify ServiceNow integration behavior end-to-end. Tests are written in Java (JUnit 4) and executed via Maven Surefire on Windows.

**What we test:**
- **CDF integration** — Custom Data Feed feature layers visible in ArcGIS Pro maps (query results, attribute fidelity, spatial accuracy)
- **ILL integration** — Indoor Location Loader geoprocessing tool execution within ArcGIS Pro (parameter validation, layer output, ServiceNow write-back verification)
- **ArcGIS Pro UI workflows** — navigation, pane interaction, ribbon commands as they relate to CDF/ILL features

**What we do NOT test:**
- ServiceNow REST API directly (covered by CDF unit tests in the integration repo)
- ILL Python logic directly (covered by `ill/tests/` in the integration repo)
- UIB (separate team, out of scope)

### Relationship to the Integration Repo

| Repo | Purpose |
|---|---|
| [`EsriPS/ServiceNow_Esri_Integration`](https://github.com/EsriPS/ServiceNow_Esri_Integration) | Source code for CDF (JS) and ILL (Python). Unit tests, linting, documentation. |
| [`sn-test-automation-poc`](https://github.com/dylanwhite-velocity/sn-test-automation-poc) | **This repo.** End-to-end UI tests that validate CDF and ILL behavior through ArcGIS Pro. |

When CDF or ILL behavior changes in the integration repo, corresponding test cases here may need updates. Cross-reference PRs by linking to the integration repo issue number.

## Repository Structure

```
sn-test-automation-poc/
├── AGENTS.md                                   # This file — agent instructions and project conventions
├── README.md                                   # Setup guide, running tests, troubleshooting
├── pom.xml                                     # Maven config, dependencies, Surefire plugin
├── .gitignore
└── src/test/java/com/esri/sn/
    ├── base/
    │   └── WinAppDriverTestBase.java           # Session lifecycle — all tests extend this
    ├── tests/
    │   └── ArcGisProLaunchTest.java            # POC: launch ArcGIS Pro, assert window
    └── utils/
        └── TestResultLogger.java               # Structured console logging via JUnit TestWatcher
```

### Key directories

| Path | Purpose |
|---|---|
| `src/test/java/com/esri/sn/base/` | Abstract base class managing WinAppDriver session lifecycle |
| `src/test/java/com/esri/sn/tests/` | Test classes — one class per feature area being tested |
| `src/test/java/com/esri/sn/utils/` | Shared test utilities (logging, helpers) |
| `test-results/` | Surefire XML reports and failure screenshots (gitignored) |

## Branching Strategy

```
main                              # Stable, reviewed tests
└── feature/<story-id>-desc       # Feature branches per story/task
```

PR titles follow: `(feat|fix|cicd|chore)/<issue-number>-short-description`

## Build & Test Commands

All commands require **Windows** with WinAppDriver running and ArcGIS Pro installed.

```cmd
:: Compile (verify dependencies, no test execution)
mvn test-compile

:: Run all tests (WinAppDriver must be running)
mvn test

:: Run with overrides
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe"
mvn test -Darcgis.pro.startup.wait.seconds=90
mvn test -Dwinappdriver.url="http://127.0.0.1:4727"

:: Generate HTML report
mvn surefire-report:report
```

### Configurable Properties

| Property | Default | Description |
|---|---|---|
| `winappdriver.url` | `http://127.0.0.1:4723` | WinAppDriver endpoint |
| `arcgis.pro.exe.path` | `C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe` | Full path to ArcGIS Pro |
| `arcgis.pro.startup.wait.seconds` | `45` | Seconds to wait after launching Pro before running tests |

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Windows | 10/11 | WinAppDriver is Windows-only |
| Developer Mode | Enabled | Required for WinAppDriver |
| JDK | 11+ | `JAVA_HOME` must be set |
| Maven | 3.6+ | `M2_HOME` on `PATH` |
| WinAppDriver | 1.2.1 | Must be running as Administrator during tests |
| ArcGIS Pro | 3.x | Must be licensed and signed in (at least once manually) |

## Issue & PR Conventions

### PR Template
- Title: `(feat|fix|cicd|chore)/<issue-number>-short-description`
- Must include: description, what CDF/ILL behavior is being tested, proof of test execution
- Cross-reference integration repo issues when applicable: `Related: EsriPS/ServiceNow_Esri_Integration#<number>`

### Git Commit Message Format
Follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):
- `feat(tests): add CDF layer visibility test`
- `fix(base): increase implicit wait for slow environments`
- `chore(deps): bump selenium to 3.x.x`

---

## Coding Standards

### Shared Standards (All Code)

- All code **must** use meaningful variable and function names
- All tests **must** pass before merging
- All code **must** be reviewed by at least one other developer before merging
- When providing credentials or passwords, **must** use the placeholder "ask a ServiceNow Integration Team member" instead of actual values
- Exception handling **should** be used to gracefully handle test setup/teardown failures
- Follow DRY, KISS, YAGNI, and Single Responsibility principles

### Java Standards

- All code **must** target Java 11+
- All code **must** use meaningful, descriptive names (e.g., `verifyFeatureLayerHasAttributes` not `test1`)
- Every public class and method **must** have a Javadoc comment describing its purpose
- Every test class **must** extend `WinAppDriverTestBase`
- Every test class **must** include the `TestResultLogger` rule:
  ```java
  @Rule
  public TestWatcher resultLogger = TestResultLogger.create();
  ```
- Test classes **must** live in `src/test/java/com/esri/sn/tests/`
- Utility classes **must** live in `src/test/java/com/esri/sn/utils/`
- Base classes **must** live in `src/test/java/com/esri/sn/base/`

### Element Location Strategy

When locating ArcGIS Pro UI elements, use this priority order:

| Priority | Strategy | Example | When to use |
|---|---|---|---|
| 1 | AccessibilityId | `driver.findElementByAccessibilityId("RibbonTabMap")` | **Preferred** — most stable across versions |
| 2 | Name | `driver.findElementByName("Map")` | When AutomationId is unavailable |
| 3 | ClassName | `driver.findElementByClassName("Button")` | For generic element types |
| 4 | XPath | `driver.findElementByXPath("//Button[@Name='OK']")` | Last resort — fragile |

Use **Inspect.exe** or **Accessibility Insights** to discover element identifiers.

### Test Class Template

```java
package com.esri.sn.tests;

import com.esri.sn.base.WinAppDriverTestBase;
import com.esri.sn.utils.TestResultLogger;
import org.junit.Assert;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.TestWatcher;

/**
 * Tests for [feature area being tested].
 *
 * <p>[What CDF/ILL behavior this validates and why it matters.]</p>
 *
 * <p><strong>Prerequisites:</strong> [Any specific setup beyond standard base class.]</p>
 */
public class FeatureAreaTest extends WinAppDriverTestBase {

    @Rule
    public TestWatcher resultLogger = TestResultLogger.create();

    /**
     * Verifies that [specific behavior under test].
     */
    @Test
    public void verifySpecificBehavior() {
        // Arrange — locate elements
        // Act — interact with the UI
        // Assert — verify expected state
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

### Technology Stack

| Layer | Technology |
|---|---|
| Test framework | JUnit 4 |
| UI automation | WinAppDriver 1.2.1 via Appium java-client |
| Build tool | Maven 3.6+ with Surefire |
| Target application | ArcGIS Pro 3.x (WPF desktop app, Windows only) |
| Integration source | ServiceNow REST API → CDF Feature Service / ILL Python Toolbox |

---

## Agent Roles

### Shared Protocol: Pause for Blockers

All agents **must** follow this protocol before proceeding with any task that depends on external information:

1. **Gather** — Read all available inputs (issues, PRs, source code, existing tests).
2. **Assess** — Determine whether you have enough information to produce an accurate, complete result.
3. **Pause if blocked** — If any required information is missing, ambiguous, or contradictory:
   - **Stop work** immediately. Do not guess or fill in gaps with assumptions.
   - Surface **specific, numbered questions** to the user. Each question should explain what is missing and why it blocks progress.
   - **Wait** for the user to respond before continuing.
4. **Resume** — Once the user answers, incorporate the answers and proceed.

### Test Developer

**Trigger:** Asked to write a new test case, extend coverage, or create test scaffolding for a CDF/ILL feature.

**Workflow:**
1. Identify which CDF/ILL feature is being tested and locate the relevant source in the integration repo
2. Determine which ArcGIS Pro UI elements are involved (check Inspect.exe output or existing tests for reference)
3. Create the test class following the Test Class Template (see Coding Standards above)
4. Ensure the test extends `WinAppDriverTestBase` and includes `TestResultLogger`
5. Use the element location strategy priority order (AccessibilityId first)
6. Verify the test compiles with `mvn test-compile`

**Rules:**
- Every test must have a clear Javadoc explaining what integration behavior it validates
- Tests must be deterministic — no reliance on transient ServiceNow data without setup/teardown
- Use `Assert` messages that explain what was expected vs what was found
- Capture screenshots on failure (handled automatically by `TestResultLogger`)
- Reference the integration repo issue number in test Javadoc when applicable

### Test Runner

**Trigger:** Asked to run tests, verify results, or diagnose failures.

**Workflow:**
1. Verify the Windows environment (WinAppDriver running, ArcGIS Pro installed)
2. Run `mvn test` (or a subset with `-Dtest=ClassName`)
3. Report results: passed/failed count, failure details, screenshot locations
4. If tests fail, examine the failure output, screenshots, and relevant source to diagnose the root cause
5. Distinguish between test bugs (our code) and integration bugs (CDF/ILL behavior changed)

**Rules:**
- Never modify test files or source code unless explicitly asked
- Report the exact error output — do not summarize away details
- For environment issues (WinAppDriver not running, Pro not found), diagnose and provide the fix command
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
- All AI-generated code must be read, understood, and tested by the person employing it
