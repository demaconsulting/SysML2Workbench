package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def RollingFileLogger {
            doc /* Writes application events to a local rolling log file. */

            metadata info : SoftwareInfo {
                description = "Writes application events to a local rolling log file.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/LoggingSubsystem/RollingFileLogger.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystem/RollingFileLoggerTests.cs");
                designFiles = ("docs/design/sysml2-workbench/logging-subsystem/rolling-file-logger.md");
                verificationFiles = ("docs/verification/sysml2-workbench/logging-subsystem/rolling-file-logger.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/logging-subsystem/rolling-file-logger.yaml");
            }
        }
    }
}
