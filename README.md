# ServiceNow Test Automation POC

WinAppDriver-based UI test harness for validating the ArcGIS Pro + Custom Data Feed (CDF) integration.

## Overview

This project uses [WinAppDriver](https://github.com/microsoft/WinAppDriver) to drive ArcGIS Pro's desktop UI through the Appium protocol. Tests are written in Java with JUnit 4 and built with Maven. Results are generated as Surefire XML/HTML reports.

**This is a proof-of-concept.** The immediate goal is to prove the full automation chain works:
`WinAppDriver → Appium java-client → JUnit 4 → ArcGIS Pro`

> **Cross-platform note:** Test code is authored on macOS and executed on Windows.
> The project compiles on any OS, but tests only run on Windows where WinAppDriver and ArcGIS Pro are installed.

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

### Step 2 — Install JDK 11+

Download and install a JDK 11 or later distribution. Recommended: [Eclipse Temurin (Adoptium)](https://adoptium.net/).

Verify after install:

```cmd
java -version
```

Expected output (version 11 or higher):

```
openjdk version "11.0.x" ...
```

> **Tip:** Ensure `JAVA_HOME` is set and `%JAVA_HOME%\bin` is on your `PATH`.

### Step 3 — Install Maven 3.6+

Download from [Maven downloads](https://maven.apache.org/download.cgi) and follow the [installation instructions](https://maven.apache.org/install.html).

Verify:

```cmd
mvn -version
```

Expected output (3.6 or higher):

```
Apache Maven 3.9.x ...
```

> **Tip:** Add `M2_HOME` and `%M2_HOME%\bin` to your `PATH`.

### Step 4 — Enable Developer Mode

1. Open **Settings → Update & Security → For developers** (Windows 10) or **Settings → System → For developers** (Windows 11)
2. Toggle **Developer Mode** to **On**

This is required for WinAppDriver to function.

### Step 5 — Install WinAppDriver

1. Download the latest `.msi` from [WinAppDriver releases](https://github.com/Microsoft/WinAppDriver/releases)
2. Run the installer (default path: `C:\Program Files (x86)\Windows Application Driver\`)

### Step 6 — Start WinAppDriver

Open a terminal **as Administrator** and run:

```cmd
"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
```

You should see:

```
Windows Application Driver listening for requests at: http://127.0.0.1:4723/
Press ENTER to exit.
```

**Leave this terminal open** while tests execute.

### Step 7 — Verify the full environment

Run this quick check to confirm everything is in place before your first test run:

```cmd
:: JDK
java -version

:: Maven
mvn -version

:: ArcGIS Pro installed
dir "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"

:: WinAppDriver responding (should show HTML or connection error if not started)
curl -s http://127.0.0.1:4723/status
```

All four commands should succeed. If any fail, revisit the corresponding step above.

### Step 8 — Compile and download dependencies (first time)

Before running tests, verify the project compiles and all Maven dependencies download:

```cmd
mvn test-compile
```

This should complete with `BUILD SUCCESS`. If it fails, check your JDK/Maven installation.

### Step 9 — Run the POC tests

```cmd
mvn test
```

If ArcGIS Pro is installed at a non-default location:

```cmd
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe"
```

You should see console output like:

```
[WinAppDriverTestBase] Waiting 45s for ArcGIS Pro to initialize...
[WinAppDriverTestBase] Startup wait complete.
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyArcGisProLaunches
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyMainWindowHasElements
```

> **Startup wait:** ArcGIS Pro takes 30–60+ seconds to launch. The base class waits 45 seconds by default.
> Override with: `mvn test -Darcgis.pro.startup.wait.seconds=60`

> **Sign-in screen:** If ArcGIS Pro shows a sign-in or licensing dialog on first launch,
> sign in manually once, close Pro, then re-run the tests. The POC test expects the main
> ArcGIS Pro window — not the sign-in dialog. Automating sign-in dismissal is a future iteration.

---

## Prerequisites (Quick Reference)

| Requirement | Version | Verify With |
|---|---|---|
| Windows 10/11 | — | — |
| Developer Mode | Enabled | Settings → For Developers |
| JDK | 11+ | `java -version` |
| Maven | 3.6+ | `mvn -version` |
| WinAppDriver | 1.2.1 | `curl http://127.0.0.1:4723/status` |
| ArcGIS Pro | 3.x | `dir "C:\Program Files\ArcGIS\Pro\bin\ArcGISPro.exe"` |

---

## Running Tests

From the project root:

```cmd
mvn test
```

### Override Default Settings

```cmd
:: Custom ArcGIS Pro path
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe"

:: Custom WinAppDriver URL
mvn test -Dwinappdriver.url="http://127.0.0.1:4727"

:: Longer startup wait (default is 45s)
mvn test -Darcgis.pro.startup.wait.seconds=90

:: All overrides combined
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe" -Dwinappdriver.url="http://127.0.0.1:4727" -Darcgis.pro.startup.wait.seconds=60
```

---

## Test Results

After `mvn test`, results land in:

```
test-results/
├── TEST-com.esri.sn.tests.ArcGisProLaunchTest.xml   # JUnit XML
└── screenshots/                                       # Auto-captured on failure
    └── ArcGisProLaunchTest_verifyArcGisProLaunches.png
```

For an HTML report:

```cmd
mvn surefire-report:report
:: Opens at target/site/surefire-report.html
```

Structured result logs also appear in the console:

```
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyArcGisProLaunches
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyMainWindowHasElements
```

> **Screenshots on failure:** When a test fails, a screenshot is automatically captured
> and saved to `test-results/screenshots/`. Check these to see what was on screen.

---

## Project Structure

```
sn-test-automation-poc/
├── src/test/java/com/esri/sn/
│   ├── base/
│   │   └── WinAppDriverTestBase.java   # Session lifecycle — all tests extend this
│   ├── tests/
│   │   └── ArcGisProLaunchTest.java    # POC: launch ArcGIS Pro, assert window
│   └── utils/
│       └── TestResultLogger.java       # Structured console logging via JUnit TestWatcher
├── pom.xml                             # Dependencies & Surefire config
├── README.md                           # This file
└── .gitignore
```

---

## Development Workflow (Mac → Windows)

| Step | Machine | Command / Action |
|---|---|---|
| 1 | Mac | Author / edit test code |
| 2 | Mac | `git push` |
| 3 | Windows VM | `git pull` |
| 4 | Windows VM | Start WinAppDriver as admin |
| 5 | Windows VM | `mvn test` |
| 6 | Windows VM | Review results in `test-results/` or `target/site/surefire-report.html` |

---

## Copilot Agent

This project has a companion Copilot agent: **`servicenow-test-developer`**.

- **Location:** `~/.copilot/agents/servicenow-test-developer.agent.md` (local to each machine)
- **Purpose:** Generates WinAppDriver test cases following this project's patterns
- **Setup:** Manually copy the agent file from your Mac to `~/.copilot/agents/` on the Windows VM

The agent will verify your Windows environment before writing tests. See the agent file for full details.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| `java` not recognized | JDK not installed or not on PATH | Install JDK 11+, set `JAVA_HOME`, add to `PATH` |
| `mvn` not recognized | Maven not installed or not on PATH | Install Maven, set `M2_HOME`, add to `PATH` |
| Connection refused on 4723 | WinAppDriver not running | Start `WinAppDriver.exe` as admin |
| "Developer Mode is not enabled" | Windows setting not toggled | Settings → For Developers → On |
| ArcGIS Pro not found | Wrong exe path | Override with `-Darcgis.pro.exe.path="..."` |
| Tests compile but fail at runtime on Mac | Expected — WinAppDriver is Windows-only | Run tests on Windows |
| Title doesn't contain "ArcGIS Pro" | Sign-in/licensing dialog on first launch | Sign in manually once, close Pro, re-run tests |
| Timeout during launch | ArcGIS Pro startup too slow | Increase wait: `-Darcgis.pro.startup.wait.seconds=90` |

---

## Writing New Tests

After the POC passes, here's how to discover UI elements and write new tests.

### 1. Discover element identifiers with Inspect.exe

ArcGIS Pro is a WPF application — most elements have `AutomationId` properties. To find them:

1. Open **Inspect.exe** from the Windows SDK:
   ```cmd
   "C:\Program Files (x86)\Windows Kits\10\bin\x64\inspect.exe"
   ```
   > **Alternative:** [Accessibility Insights for Windows](https://accessibilityinsights.io/docs/windows/overview/) provides a friendlier UI.

2. Launch ArcGIS Pro normally (not through WinAppDriver)
3. In Inspect.exe, hover over UI elements in ArcGIS Pro
4. Note the **AutomationId**, **Name**, and **ClassName** for each element you want to interact with
5. Use these identifiers in your test code (see priority order below)

### 2. Element location strategy (priority order)

| Strategy | Example | When to use |
|---|---|---|
| AccessibilityId | `driver.findElementByAccessibilityId("RibbonTabMap")` | **Preferred** — most stable across versions |
| Name | `driver.findElementByName("Map")` | When AutomationId is unavailable |
| ClassName | `driver.findElementByClassName("Button")` | For generic element types |
| XPath | `driver.findElementByXPath("//Button[@Name='OK']")` | Last resort — fragile |

### 3. Create the test class

Every test class must:
- Live in `src/test/java/com/esri/sn/tests/`
- Extend `WinAppDriverTestBase`
- Include the `TestResultLogger` rule
- Have a Javadoc comment explaining what it validates

See the agent (`servicenow-test-developer`) for the full class template and patterns.

---

## Related Resources

- [WinAppDriver Documentation](https://github.com/microsoft/WinAppDriver/tree/master/Docs)
- [WinAppDriver Java Samples](https://github.com/microsoft/WinAppDriver/tree/master/Samples/Java)
- [Authoring Test Scripts](https://github.com/microsoft/WinAppDriver/blob/master/Docs/AuthoringTestScripts.md)
- [Inspect.exe (UI element inspector)](https://learn.microsoft.com/en-us/windows/win32/winauto/inspect-objects)
- [Accessibility Insights for Windows](https://accessibilityinsights.io/docs/windows/overview/)
- [ArcGIS Enterprise CDF Documentation](https://developers.arcgis.com/enterprise-sdk/guide/custom-data-feeds/)
- [Esri CUIT Repo (internal)](https://devtopia.esri.com/ArcGISPro/cuit/) — team's existing WinAppDriver tests for ArcGIS Pro

---

## Changelog

> Keep this section up to date as the project evolves. Newest entries at the top.

| Date | Change |
|---|---|
| 2026-04-07 | Add startup wait (45s default), screenshot-on-failure, compile step, Inspect.exe workflow, sign-in troubleshooting |
| 2026-04-07 | Initial POC scaffold: Maven project, base class, launch test, result logger, README |

