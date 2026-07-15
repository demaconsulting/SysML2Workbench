## LoggingSubsystem

![LoggingSubsystem Structure](LoggingSubsystemView.svg)

### Overview

LoggingSubsystem provides local persistent logging for troubleshooting and
user-attachable bug reports. Its boundary starts with structured application
events emitted by other subsystems and ends with a bounded rolling set of log
files on disk. It contains one unit, RollingFileLogger. Centralized analysis,
telemetry, and network log shipping are explicitly outside the subsystem scope.

### Interfaces

**Application Logging API**: In-process interface for recording operational
events and failures.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts log level, message text, and optional exception context
  from any subsystem in the application.
- *Constraints*: Logging must be best-effort, must not block foreground UI work
  longer than necessary, and must retain ordering within a single file.

**Log File Store**: The local persistent destination for rolling log files.

- *Type*: File system.
- *Role*: Consumer.
- *Contract*: Writes timestamped log records to a bounded set of files under a
  local directory accessible to the user.
- *Constraints*: Rotation must cap disk usage and must tolerate missing or
  locked log files by surfacing the failure without crashing the application.

### Design

1. Application subsystems emit structured events to RollingFileLogger through a
   common logging API.
2. RollingFileLogger formats each event, appends it to the active file, and
   checks whether a rotation threshold has been reached.
3. When the active file exceeds policy limits, the logger rolls to a new file
   and deletes or archives files beyond the retention count.
4. AppShellSubsystem may expose the log location or attach instructions to
   support bug report workflows, but the subsystem itself remains a local file
   writer.
