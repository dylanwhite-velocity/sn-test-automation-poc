package com.esri.sn.tests;

import com.esri.sn.base.WinAppDriverTestBase;
import com.esri.sn.utils.TestResultLogger;
import org.junit.Assert;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.TestWatcher;
import org.openqa.selenium.WebElement;

/**
 * POC test that validates the WinAppDriver → ArcGIS Pro launch chain.
 *
 * <p>This test proves that:</p>
 * <ol>
 *   <li>WinAppDriver can launch ArcGIS Pro via Appium capabilities</li>
 *   <li>The main ArcGIS Pro window is reachable from the driver session</li>
 *   <li>JUnit 4 + Surefire reports results correctly</li>
 * </ol>
 *
 * <p><strong>Prerequisites:</strong> WinAppDriver running on Windows,
 * ArcGIS Pro installed at the configured path.</p>
 */
public class ArcGisProLaunchTest extends WinAppDriverTestBase {

    @Rule
    public TestWatcher resultLogger = TestResultLogger.create();

    /**
     * Verifies that ArcGIS Pro launches and the main window title is present.
     *
     * <p>The window title for ArcGIS Pro typically contains "ArcGIS Pro"
     * (either the start page or an open project). This assertion confirms
     * the driver session successfully targeted the application.</p>
     */
    @Test
    public void verifyArcGisProLaunches() {
        Assert.assertNotNull(
                "Driver session should be established",
                driver);

        String windowTitle = driver.getTitle();
        Assert.assertNotNull(
                "Window title should not be null",
                windowTitle);
        Assert.assertTrue(
                "Window title should contain 'ArcGIS Pro', got: " + windowTitle,
                windowTitle.contains("ArcGIS Pro"));
    }

    /**
     * Verifies that the ArcGIS Pro main window has at least one child element.
     *
     * <p>This is a secondary smoke check confirming that the UI element tree
     * is accessible through WinAppDriver, which is required for all subsequent
     * UI automation tests.</p>
     */
    @Test
    public void verifyMainWindowHasElements() {
        Assert.assertNotNull(
                "Driver session should be established",
                driver);

        // The ArcGIS Pro main window should have a root element
        WebElement rootElement = driver.findElementByXPath("/*");
        Assert.assertNotNull(
                "Root window element should exist",
                rootElement);
    }
}
