package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def LoggingSubsystem {
            doc /* Local rolling log file for user-attachable bug reports. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/logging-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/logging-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/logging-subsystem.yaml */

            part rollingFileLogger: RollingFileLogger;
        }
    }
}
