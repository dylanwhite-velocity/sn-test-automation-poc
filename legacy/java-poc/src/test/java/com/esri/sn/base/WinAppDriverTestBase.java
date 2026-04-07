package com.esri.sn.base;

import io.appium.java_client.windows.WindowsDriver;
import org.junit.AfterClass;
import org.junit.BeforeClass;
import org.openqa.selenium.OutputType;
import org.openqa.selenium.TakesScreenshot;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.remote.DesiredCapabilities;

import java.io.File;
import java.io.IOException;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
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
 *
 * <p><strong>ArcGIS Pro startup:</strong> Pro can take 30–60+ seconds to fully launch.
 * After session creation, this base class waits for the configured startup delay
 * ({@code arcgis.pro.startup.wait.seconds}, default 45) to allow the application to
 * initialize before tests begin.</p>
 *
 * <p><strong>Sign-in screen:</strong> On first launch or after updates, ArcGIS Pro may
 * display a sign-in / licensing dialog instead of the main window. If the POC launch
 * test fails because the window title doesn't contain "ArcGIS Pro", sign in manually
 * once, then re-run the tests. Future iterations can automate sign-in dismissal.</p>
 */
public abstract class WinAppDriverTestBase {

    /** Default WinAppDriver endpoint. */
    private static final String DEFAULT_WINAPPDRIVER_URL = "http://127.0.0.1:4723";

    /** Default ArcGIS Pro executable path. */
    private static final String DEFAULT_ARCGIS_PRO_PATH =
            "C:\\Program Files\\ArcGIS\\Pro\\bin\\ArcGISPro.exe";

    /** Implicit wait timeout in seconds applied to the driver session. */
    private static final int IMPLICIT_WAIT_SECONDS = 10;

    /** Default seconds to wait for ArcGIS Pro to finish launching. */
    private static final int DEFAULT_STARTUP_WAIT_SECONDS = 45;

    /** Directory for failure screenshots (relative to project root). */
    private static final String SCREENSHOT_DIR = "test-results/screenshots";

    /** The WinAppDriver session shared by all tests in a class. */
    protected static WindowsDriver<WebElement> driver;

    /**
     * Starts the WinAppDriver session before any test methods run.
     *
     * <p>Reads configuration from system properties, launches ArcGIS Pro
     * through WinAppDriver, and waits for the application to initialize.</p>
     *
     * @throws Exception if the session cannot be created
     */
    @BeforeClass
    public static void setupSession() throws Exception {
        String winAppDriverUrl = System.getProperty(
                "winappdriver.url", DEFAULT_WINAPPDRIVER_URL);
        String arcGisProPath = System.getProperty(
                "arcgis.pro.exe.path", DEFAULT_ARCGIS_PRO_PATH);
        int startupWait = Integer.parseInt(System.getProperty(
                "arcgis.pro.startup.wait.seconds",
                String.valueOf(DEFAULT_STARTUP_WAIT_SECONDS)));

        DesiredCapabilities capabilities = new DesiredCapabilities();
        capabilities.setCapability("app", arcGisProPath);

        driver = new WindowsDriver<>(new URL(winAppDriverUrl), capabilities);
        driver.manage().timeouts().implicitlyWait(IMPLICIT_WAIT_SECONDS, TimeUnit.SECONDS);

        // ArcGIS Pro takes a significant time to launch — wait before running tests
        if (startupWait > 0) {
            System.out.println("[WinAppDriverTestBase] Waiting " + startupWait
                    + "s for ArcGIS Pro to initialize...");
            Thread.sleep(startupWait * 1000L);
            System.out.println("[WinAppDriverTestBase] Startup wait complete.");
        }
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

    /**
     * Captures a screenshot from the current driver session and saves it to
     * {@code test-results/screenshots/<filename>.png}.
     *
     * <p>Call this from test code or from a {@code TestWatcher.failed()} hook
     * to capture the screen state when a test fails.</p>
     *
     * @param filename base name for the screenshot file (without extension)
     * @return the path to the saved screenshot, or null if capture failed
     */
    protected static Path captureScreenshot(String filename) {
        if (driver == null) {
            System.err.println("[WinAppDriverTestBase] Cannot capture screenshot — driver is null");
            return null;
        }
        try {
            File screenshotFile = ((TakesScreenshot) driver).getScreenshotAs(OutputType.FILE);
            Path screenshotDir = Paths.get(SCREENSHOT_DIR);
            Files.createDirectories(screenshotDir);
            Path destination = screenshotDir.resolve(filename + ".png");
            Files.copy(screenshotFile.toPath(), destination, StandardCopyOption.REPLACE_EXISTING);
            System.out.println("[WinAppDriverTestBase] Screenshot saved: " + destination);
            return destination;
        } catch (IOException e) {
            System.err.println("[WinAppDriverTestBase] Screenshot capture failed: " + e.getMessage());
            return null;
        }
    }
}
