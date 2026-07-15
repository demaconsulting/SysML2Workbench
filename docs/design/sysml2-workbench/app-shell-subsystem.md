## AppShellSubsystem

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

### Overview

AppShellSubsystem is the composition root for the desktop application. It owns
the main window, navigation flow, and tabbed viewing experience that tie the
other subsystems into one coherent user workflow. Its only documented unit is
MainWindowShell. The thin desktop platform head remains outside the documented
unit set because it is only a bootstrap and lifetime host.

### Interfaces

**Workbench Shell API**: The in-process command surface that coordinates user
actions across subsystems.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Opens workspaces, manages active tabs, selects predefined views,
  previews custom views, exposes diagnostics, and routes logging-visible
  failures to the user.
- *Constraints*: The shell must preserve UI responsiveness and must keep
  subsystem orchestration deterministic even when the workspace reloads
  mid-session. Notifications the Avalonia UI layer consumes (`TabsChanged`)
  are marshaled onto the UI thread regardless of which thread raises them,
  since shell operations such as opening a workspace may complete on a
  background continuation after an asynchronous workspace load.

**Application Lifetime Host**: The platform-specific startup and shutdown
environment.

- *Type*: Desktop host integration.
- *Role*: Consumer.
- *Contract*: Consumes startup arguments, initializes Avalonia application
  lifetime, and disposes subsystem resources when the process exits.
- *Constraints*: Host integration must remain thin and must not absorb
  subsystem responsibilities that belong in the shared UI project.

### Design

1. MainWindowShell creates the user-facing composition of workspace loading,
   view selection, custom-view editing, diagnostics display, and diagram
   presentation.
2. The shell forwards workspace commands to WorkspaceSubsystem and listens for
   updated workspace, diagnostic, and view-catalog state.
3. It routes predefined selections to ViewCatalogSubsystem, custom preview
   requests to ViewBuilderSubsystem, and render requests to
   LayoutRenderingSubsystem.
4. It binds DiagnosticsPanelSubsystem and LoggingSubsystem outputs into the
   user workflow without pushing UI-specific code into those subsystems.
5. The desktop platform head initializes the shell and application lifetime but
   remains a bootstrap concern rather than a separately documented unit.
6. The Avalonia UI layer presents MainWindowShell's panels (predefined
   views, custom view builder, diagnostics, and diagram) as a resizable,
   floatable Dock layout, composed by a Dock factory from thin panel views
   and view models that hold no logic beyond forwarding to MainWindowShell
   and binding its state. The three Tool panels (predefined views, custom
   view builder, diagnostics) are closable and restorable via a View menu,
   which reuses the same long-lived panel view model instance so its
   in-progress state survives the close/restore cycle. The diagram area
   hosts zero or more independently closable diagram Documents - one per
   open `WorkbenchTab`, each bound to its own `SvgCanvasHost` - and the
   Dock `DocumentDock` container itself remains visibly present even when
   no diagram tab is open, so an empty diagram area is a normal, supported
   state rather than a dead end. MainWindowShell itself has no dependency
   on Avalonia or Dock and is unaware of how its panels are arranged on
   screen.
