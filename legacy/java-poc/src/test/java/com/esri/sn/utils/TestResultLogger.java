package com.esri.sn.utils;

import com.esri.sn.base.WinAppDriverTestBase;
import org.junit.rules.TestWatcher;
import org.junit.runner.Description;

/**
 * JUnit 4 {@link TestWatcher} that logs structured test results to stdout
 * and captures a screenshot on failure.
 *
 * <p>Attach as a {@code @Rule} in every test class to get human-readable,
 * structured output alongside the Surefire XML/HTML reports.</p>
 *
 * <pre>{@code
 * @Rule
 * public TestWatcher resultLogger = TestResultLogger.create();
 * }</pre>
 */
public final class TestResultLogger extends TestWatcher {

    private TestResultLogger() {
        // Use factory method
    }

    /**
     * Creates a new {@link TestResultLogger} instance.
     *
     * @return a new TestResultLogger to use as a JUnit {@code @Rule}
     */
    public static TestResultLogger create() {
        return new TestResultLogger();
    }

    @Override
    protected void succeeded(Description description) {
        logResult(description, "PASSED", null);
    }

    @Override
    protected void failed(Throwable e, Description description) {
        logResult(description, "FAILED", e.getMessage());

        // Capture screenshot on failure for debugging
        String screenshotName = description.getTestClass().getSimpleName()
                + "_" + description.getMethodName();
        WinAppDriverTestBase.captureScreenshot(screenshotName);
    }

    @Override
    protected void skipped(org.junit.AssumptionViolatedException e, Description description) {
        logResult(description, "SKIPPED", e.getMessage());
    }

    /**
     * Writes a structured log line to stdout.
     *
     * @param description the JUnit test description
     * @param status      PASSED, FAILED, or SKIPPED
     * @param detail      optional detail message (may be null)
     */
    private static void logResult(Description description, String status, String detail) {
        String testName = description.getTestClass().getSimpleName()
                + "#" + description.getMethodName();
        StringBuilder sb = new StringBuilder();
        sb.append("[TEST RESULT] ")
          .append(status)
          .append(" | ")
          .append(testName);
        if (detail != null && !detail.isEmpty()) {
            sb.append(" | ").append(detail);
        }
        System.out.println(sb.toString());
    }
}
