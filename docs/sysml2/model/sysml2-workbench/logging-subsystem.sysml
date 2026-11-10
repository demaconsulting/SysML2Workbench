package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def LoggingSubsystem {
            doc /* Local rolling log file for user-attachable bug reports. */

            metadata info : SoftwareInfo {
                description = "Local rolling log file for user-attachable bug reports.";
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/logging-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/logging-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/logging-subsystem.yaml");
            }

            part rollingFileLogger: RollingFileLogger;
        }
    }
}
