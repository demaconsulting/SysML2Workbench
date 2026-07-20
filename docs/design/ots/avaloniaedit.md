## AvaloniaEdit

### Purpose

AvaloniaEdit was chosen to provide read-only, syntax-highlighted text
viewing of a `.sysml` file's raw source without the workbench hand-rolling
its own text control, line-numbered gutter, or highlighting engine. It backs
the source-text viewer's document tabs, giving a user a faithful, readable
view of a file's exact on-disk text alongside its rendered diagrams.

### Features Used

- **`AvaloniaEdit.TextEditor`** — the hosted text control, configured
  `IsReadOnly = true` so the source-text viewer can never mutate the
  underlying file.
- **`AvaloniaEdit.Highlighting.Xshd.HighlightingLoader`** — loads a
  self-authored XSHD v2 highlighting definition from an embedded
  `AvaloniaResource` at runtime.
- **`AvaloniaEdit.Highlighting.HighlightingManager`** — registers the
  loaded definition once, lazily, and resolves any XSHD-internal
  references during load.

### Integration Pattern

`SourceTextDocumentView`'s code-behind loads and registers the embedded
`avares://DemaConsulting.SysML2Workbench/Assets/SysML.xshd` highlighting
definition once, via a static, lazily-initialized `IHighlightingDefinition`
shared across every open source-text tab, and assigns it to the hosted
`TextEditor.SyntaxHighlighting` property. The XSHD file itself is generic
(comment, string, and number rules only); the keyword-highlighting rule is
built at load time by reflecting
`DemaConsulting.SysML2Tools.Parser.Antlr.SysMLv2Lexer`'s literal-token table
rather than hand-copying a keyword list that could drift from the real
grammar, with a hard-coded fallback keyword list used if the reflection
call ever fails. `TextEditor.Text` is a plain CLR property rather than an
Avalonia styled property, so the view model's `Text` is assigned to it
imperatively in code-behind rather than through an XAML binding; this is
safe because the source-text viewer never changes `Text` after
construction. There is no write-back path in this phase.
