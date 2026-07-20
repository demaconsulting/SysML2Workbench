### AboutDialog

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

AboutDialog is the modal dialog opened from the main window's Help menu that
tells the user which application they are running, which build, and what it
is built on. It is documented as one unit covering both
`AboutDialogViewModel` (the Avalonia-free data the dialog presents) and
`AboutDialogView` (the Avalonia `Window` shown via `ShowDialog`), matching
the pairing convention used by the other panel units in this subsystem
(`WorkspacePanelToolViewModel`/`View`, `PredefinedViewsToolViewModel`/`View`).
Unlike the four Dock tool panels, AboutDialog is not a Dock tool: it is a
short-lived, owned dialog `Window`, the first one in the application besides
`MainWindowView` itself.

#### Data Model

**ApplicationName**: `string` — the application's fixed display name,
`"SysML2Workbench"`.

**Tagline**: `string` — the application's fixed one-line description,
`"Cross-platform desktop viewer and IDE for SysML v2 models"`.

**VersionText**: `string` — the running assembly's build-stamped version,
resolved once at construction. Prefers
`AssemblyInformationalVersionAttribute.InformationalVersion` (the full
version string CI stamps via `--property:Version=...`, including any
pre-release/build-metadata suffix); falls back to the assembly's normalized
`AssemblyName.Version` when that attribute is absent (for example an
un-stamped local build); falls back to `"Unknown"` if neither is available.
Never blank.

**Copyright**: `string` — the application's fixed copyright text,
`"Copyright (c) 2026 DEMA Consulting"`, matching the repository's own
`LICENSE` file.

**Dependencies**: `IReadOnlyList<DependencyInfo>` — the application's key OSS
dependencies, each a `DependencyInfo` record with `Name` and `License`.
Populated once, at construction, with a fixed list covering every directly
referenced package that underpins the running application: `Avalonia`,
`Dock.Avalonia`, `Material.Icons.Avalonia`, `CommunityToolkit.Mvvm`,
`DemaConsulting.SysML2Tools`, and `DemaConsulting.Rendering` — every entry is
MIT-licensed, so every `License` value is `"MIT License"`.

**DependencyInfo**: `record` with `Name` (package or project name) and
`License` (license type) — one row in the dependency list.

#### Key Methods

AboutDialogViewModel has no mutable methods beyond construction: every
property above is populated once, at construction time, and never changes
for the lifetime of the dialog. `AboutDialogView`'s only behavior beyond data
binding is its OK button's click handler, which closes the dialog.

#### Error Handling

There is no failure path a user can observe: `VersionText`'s resolution
already has an in-process fallback chain (informational version, then
assembly version, then a literal `"Unknown"`) so it can never throw or
render blank, and every other property is a fixed literal. AboutDialog
performs no I/O and has no dependency on workspace or subsystem state that
could be unavailable.

#### Dependencies

- **.NET reflection APIs** — `Assembly.GetExecutingAssembly()` and
  `AssemblyInformationalVersionAttribute` resolve `VersionText` from the
  running build.
- **Avalonia** — `AboutDialogView` is a `Window` shown modally via
  `ShowDialog`, using the same application icon resource as
  `MainWindowView`.

#### Callers

- **MainWindowView** — constructs `AboutDialogView` and calls
  `ShowDialog(this)` when the user selects Help > About, so the dialog is
  owned by and centers over the main window.
