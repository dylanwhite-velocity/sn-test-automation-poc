# ServiceNow Test Automation POC

WinAppDriver-based UI test harness for validating the ArcGIS Pro + Custom Data Feed (CDF) integration.

## Overview

This project uses [WinAppDriver](https://github.com/microsoft/WinAppDriver) to drive ArcGIS Pro's desktop UI through the Appium protocol. Tests are written in Java with JUnit 4 and built with Maven. Results are generated as Surefire XML/HTML reports.

**This is a proof-of-concept.** The immediate goal is to prove the full automation chain works:
`WinAppDriver → Appium java-client → JUnit 4 → ArcGIS Pro`

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Windows 10/11 | — | WinAppDriver is Windows-only |
| Developer Mode | Enabled | Settings → Update & Security → For Developers |
| JDK | 11+ | `java -version` to verify |
| Maven | 3.6+ | `mvn -version` to verify |
| WinAppDriver | 1.2.1 | See install steps below |
| ArcGIS Pro | 3.x | Default install path assumed |

## Windows Setup

### 1. Install WinAppDriver

1. Download the latest installer from [WinAppDriver releases](https://github.com/Microsoft/WinAppDriver/releases)
2. Run the `.msi` installer
3. Default install location: `C:\Program Files (x86)\Windows Application Driver\`

### 2. Enable Developer Mode

1. Open **Settings → Update & Security → For developers**
2. Toggle **Developer Mode** to **On**

### 3. Start WinAppDriver

Open a terminal **as Administrator** and run:

```cmd
"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
```

You should see:

```
Windows Application Driver listening for requests at: http://127.0.0.1:4723/
Press ENTER to exit.
```

Leave this running while tests execute.

## Running Tests

From the project root:

```bash
mvn test
```

### Override Default Settings

```bash
# Custom ArcGIS Pro path
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe"

# Custom WinAppDriver URL
mvn test -Dwinappdriver.url="http://127.0.0.1:4727"

# Both
mvn test -Darcgis.pro.exe.path="D:\ArcGIS\Pro\bin\ArcGISPro.exe" -Dwinappdriver.url="http://127.0.0.1:4727"
```

## Test Results

After running `mvn test`, results are generated in:

```
test-results/
├── TEST-com.esri.sn.tests.ArcGisProLaunchTest.xml   # JUnit XML
```

For an HTML report:

```bash
mvn surefire-report:report
# Opens at target/site/surefire-report.html
```

Structured result logs also appear in the console output:

```
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyArcGisProLaunches
[TEST RESULT] PASSED | ArcGisProLaunchTest#verifyMainWindowHasElements
```

## Project Structure

```
src/test/java/com/esri/sn/
├── base/
│   └── WinAppDriverTestBase.java   # Session lifecycle — all tests extend this
├── tests/
│   └── ArcGisProLaunchTest.java    # POC: launch ArcGIS Pro, assert window
└── utils/
    └── TestResultLogger.java       # Structured console logging via JUnit TestWatcher
```

## Development Workflow (Mac → Windows)

1. **Mac:** Author/edit test code
2. **Mac:** `git push`
3. **Windows VM:** `git pull`
4. **Windows VM:** Start WinAppDriver as admin
5. **Windows VM:** `mvn test`
6. **Windows VM:** Review results in `test-results/` or `target/site/surefire-report.html`

## Related Resources

- [WinAppDriver Documentation](https://github.com/microsoft/WinAppDriver/tree/master/Docs)
- [WinAppDriver Java Samples](https://github.com/microsoft/WinAppDriver/tree/master/Samples/Java)
- [Authoring Test Scripts](https://github.com/microsoft/WinAppDriver/blob/master/Docs/AuthoringTestScripts.md)
- [Inspect.exe (UI element inspector)](https://learn.microsoft.com/en-us/windows/win32/winauto/inspect-objects)
- [ArcGIS Enterprise CDF Documentation](https://developers.arcgis.com/enterprise-sdk/guide/custom-data-feeds/)

