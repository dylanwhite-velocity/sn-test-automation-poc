package com.esri.sn.base;

import io.appium.java_client.windows.WindowsDriver;
import org.junit.AfterClass;
import org.junit.BeforeClass;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.remote.DesiredCapabilities;

import java.net.URL;
import java.util.concurrent.TimeUnit;

/**
 * Base class for all WinAppDriver test classes.
 *
 * <p>Manages the WinAppDriver session lifecycle: creates a {@link WindowsDriver}
 * session in {@code @BeforeClass} targeting ArcGIS Pro and tears it down in
 * {@code @AfterClass}.</p>
 *
 * <p>Configuration is driven by system properties (set in {@code pom.xml} or
 * overridden on the command line):</p>
 * <ul>
 *   <li>{@code winappdriver.url} — WinAppDriver endpoint (default {@code http://127.0.0.1:4723})</li>
 *   <li>{@code arcgis.pro.exe.path} — full path to ArcGISPro.exe</li>
 * </ul>
 */
public abstract class WinAppDriverTestBase {

    /** Default WinAppDriver endpoint. */
    private static final String DEFAULT_WINAPPDRIVER_URL = "http://127.0.0.1:4723";

    /** Default ArcGIS Pro executable path. */
    private static final String DEFAULT_ARCGIS_PRO_PATH =
            "C:\\Program Files\\ArcGIS\\Pro\\bin\\ArcGISPro.exe";

    /** Implicit wait timeout in seconds applied to the driver session. */
    private static final int IMPLICIT_WAIT_SECONDS = 10;

    /** The WinAppDriver session shared by all tests in a class. */
    protected static WindowsDriver<WebElement> driver;

    /**
     * Starts the WinAppDriver session before any test methods run.
     *
     * <p>Reads configuration from system properties and launches ArcGIS Pro
     * through WinAppDriver.</p>
     *
     * @throws Exception if the session cannot be created
     */
    @BeforeClass
    public static void setupSession() throws Exception {
        String winAppDriverUrl = System.getProperty(
                "winappdriver.url", DEFAULT_WINAPPDRIVER_URL);
        String arcGisProPath = System.getProperty(
                "arcgis.pro.exe.path", DEFAULT_ARCGIS_PRO_PATH);

        DesiredCapabilities capabilities = new DesiredCapabilities();
        capabilities.setCapability("app", arcGisProPath);

        driver = new WindowsDriver<>(new URL(winAppDriverUrl), capabilities);
        driver.manage().timeouts().implicitlyWait(IMPLICIT_WAIT_SECONDS, TimeUnit.SECONDS);
    }

    /**
     * Tears down the WinAppDriver session after all test methods complete.
     */
    @AfterClass
    public static void tearDownSession() {
        if (driver != null) {
            driver.quit();
            driver = null;
        }
    }
}
