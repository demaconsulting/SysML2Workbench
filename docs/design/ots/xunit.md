## XUnit

SysML2Workbench uses xUnit v3 as the verification framework for unit- and
subsystem-level tests that qualify the local design and its OTS integrations.

### Purpose

xUnit v3 was chosen because it provides a modern .NET testing model with strong
support for isolated unit tests, parameterized cases, and integration-friendly
assertions. It gives the repository a consistent way to verify workspace
loading, diagnostics aggregation, custom-view generation, and rendering
integration without affecting runtime architecture.

### Features Used

- **Fact and theory tests** — express deterministic unit and subsystem
  scenarios.
- **Assertion APIs** — validate rendered, diagnostic, and stateful outcomes.
- **Fixture support** — shares setup for integration-style verification of OTS-
  backed workflows.
- **Test discovery and execution** — runs the repository's verification suite
  through the .NET test toolchain.

### Integration Pattern

The repository consumes xUnit v3 only inside its verification projects.
Production code has no runtime dependency on the framework. Test classes
arrange local units and their OTS collaborators, execute representative
workflows, and assert on observable outputs such as diagnostics, generated
SysML snippets, or rendered SVG. Framework initialization follows the normal
.NET test runner lifecycle, with fixture disposal handled through xUnit's
standard cleanup conventions.
