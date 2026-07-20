### AboutDialog

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/AboutDialogViewModelTests.cs` construct
`AboutDialogViewModel` directly - it has no Avalonia dependency, so no UI thread or dialog `Window` is needed to
exercise its data. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/app-shell-subsystem/about-dialog.yaml` and describes the implemented tests in
present tense. The Help menu's wiring and the OK button's close behavior in `MainWindowView`'s and
`AboutDialogView`'s code-behind are not independently unit-tested: `OnAboutMenuItemClick` only constructs
`AboutDialogView` and calls `ShowDialog(this)`, and the OK button's handler only calls `Close()` - both are verified
by code review rather than a dedicated Avalonia UI-automation test, matching the drag-and-drop precedent in
`workspace-panel.md`.

#### Test Environment

Tests run under the standard .NET test runner, constructing `AboutDialogViewModel` with no external services,
temporary directories, or collaborator units required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/AboutDialogViewModelTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/about-dialog.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**Construction_ExposesApplicationNameAndTagline**: A freshly constructed view model exposes the fixed application
name `"SysML2Workbench"` and tagline `"Cross-platform desktop viewer and IDE for SysML v2 models"`. Verified by
`AboutDialogViewModelTests.Construction_ExposesApplicationNameAndTagline`.

**Construction_ExposesVersionFromAssembly**: A freshly constructed view model's `VersionText` is non-blank and
matches the running assembly's own `AssemblyInformationalVersionAttribute` (or, absent that attribute, its
`AssemblyName.Version`). Verified by `AboutDialogViewModelTests.Construction_ExposesVersionFromAssembly`.

**Construction_ExposesCopyrightText**: A freshly constructed view model exposes the fixed copyright text
`"Copyright (c) 2026 DEMA Consulting"`, matching the repository's own `LICENSE` file. Verified by
`AboutDialogViewModelTests.Construction_ExposesCopyrightText`.

**Construction_ExposesDependencyList_ContainsExpectedEntries**: A freshly constructed view model's `Dependencies`
list contains an entry for each of the application's key OSS dependencies (`Avalonia`, `Dock.Avalonia`,
`Material.Icons.Avalonia`, `CommunityToolkit.Mvvm`, `DemaConsulting.SysML2Tools`, `DemaConsulting.Rendering`), each
paired with `"MIT License"`. Verified by
`AboutDialogViewModelTests.Construction_ExposesDependencyList_ContainsExpectedEntries`.
