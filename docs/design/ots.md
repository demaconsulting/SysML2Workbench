# OTS Dependencies

SysML2Workbench adopts a thin-shell strategy: local code orchestrates mature
external libraries instead of reimplementing parsing, layout, rendering, UI,
or test-framework concerns. Each OTS item is documented individually in the
`ots/` folder, while this section captures the common selection, upgrade,
integration, and qualification rules applied across the repository.

## Selection Criteria

OTS items are selected only when their licenses are compatible with
distribution of the repository and any downstream internal reuse. Preference is
given to dependencies with clear public documentation, active maintenance, and
enough maturity to avoid forcing the local system to compensate for unstable
behavior. Security posture is reviewed through the dependency's public issue
history, release practices, and the absence of known unacceptable
vulnerabilities for the intended desktop-only usage. Where vendor or upstream
self-validation evidence exists, it is used as supporting evidence, but the
repository still requires local integration design and verification for the
specific consumed features.

## Version Management Policy

OTS upgrades are initiated through normal dependency maintenance activity,
including manual review of new releases and any automated update proposals
adopted by the repository. Changes that alter public APIs, hosting patterns,
layout behavior, or test semantics trigger a design review of the affected
integration documents before adoption. Reproducible builds are maintained
through pinned package references and the repository's checked-in dependency
configuration rather than ad hoc workstation state.

## General Integration Approach

OTS items are consumed through narrow integration seams. SysML2Tools and
DemaConsulting.Rendering are wrapped behind local units that translate
workspace or view state into library calls. Avalonia is used as the application
shell and control layer, with Dock providing the resizable, floatable,
closable panel-docking layout for that shell and CommunityToolkit.Mvvm
supplying observable-property generation for the Dock panel view models, but
subsystem responsibilities remain in local units rather than in code-behind
spread across the UI. xUnit is confined to verification projects and does not
participate in runtime behavior. Across all items, the local code treats OTS
failures as diagnosable events: exceptions are surfaced to the shell, and
notable integration faults are logged through the local rolling logger.

## Qualification Strategy

OTS items are qualified through a combination of upstream maturity evidence and
local verification targeted at the actually consumed features. Before accepting
an upgrade, maintainers review breaking-change notes, security advisories, and
any behavioral areas directly exercised by the repository's tests. Runtime-
facing dependencies are further qualified by exercising representative
workspace load, rendering, and UI flows, while xUnit is qualified by continued
successful execution of the repository's verification suite.
