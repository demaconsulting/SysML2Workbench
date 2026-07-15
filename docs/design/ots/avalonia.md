## Avalonia

SysML2Workbench uses Avalonia 12 as the cross-platform desktop UI framework
for the application shell, control tree, data binding, and input handling.

### Purpose

Avalonia 12 was chosen because it supports a single .NET UI codebase across
Windows, Linux, and macOS while fitting the repository's desktop-only scope. It
provides the windowing, control composition, and binding infrastructure needed
for the main shell, diagnostics panel, and diagram canvas without requiring
platform-specific UI stacks.

### Features Used

- **Application lifetime hosting** — starts and shuts down the desktop
  application.
- **Window and control composition** — builds the main shell, catalog, builder,
  and diagnostics surfaces.
- **Data binding** — keeps shell and subsystem state synchronized with visible
  UI.
- **Pointer and keyboard input** — supports diagram pan and zoom plus workspace
  and view interactions.

### Integration Pattern

MainWindowShell and the other UI-facing units consume Avalonia directly through
the shared application project, while the desktop head supplies the platform-
specific host bootstrap. The framework is initialized once during process
startup and remains active for the application's lifetime. UI-affecting
callbacks from file watching or rendering are marshaled back onto the Avalonia
dispatcher before mutating bound state or controls.
