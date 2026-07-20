## AvaloniaEdit

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/AvaloniaEditTests.cs` verify AvaloniaEdit's read-only enforcement
and highlighting-load behavior directly: `[AvaloniaFact]` tests host a real, headless `AvaloniaEdit.TextEditor` to
confirm its `IsReadOnly` mode installs a section provider that genuinely rejects insertion, and that the embedded
`Assets/SysML.xshd` highlighting definition loads via `HighlightingLoader.Load` without throwing and defines a
keyword-highlighting rule (built the same way `SourceTextDocumentView` builds it at runtime, by reflecting the real
SysML v2 lexer's literal-token table) that matches a genuine SysML v2 keyword. Unit tests in
`test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/SourceTextDocumentViewModelTests.cs` additionally
verify that a file's text is faithfully surfaced, unmodified, to the editor's view model.

### Test Scenarios

**TextEditor_IsReadOnly_PreventsEdits**: A real, headless-hosted `TextEditor` with `IsReadOnly = true` installs a
`TextArea.ReadOnlySectionProvider` whose `CanInsert` rejects insertion anywhere in the document, while a normal
(non-read-only) editor's provider allows it - proving the read-only "View Source" tab genuinely cannot be edited
through the editor's own input pipeline.

**EmbeddedXshd_Loads_AndKeywordRuleMatchesSysmlKeyword**: The embedded `Assets/SysML.xshd` resource loads via
`HighlightingLoader.Load` without throwing and defines the expected named colors (Keyword, Comment, String,
Number); a keyword-highlighting rule built the same way `SourceTextDocumentView` builds it at runtime - by
independently reflecting `SysMLv2Lexer._LiteralNames` - matches a real SysML v2 keyword (`part`).

**Text_OpenedFile_MatchesFileContents**: `SourceTextDocumentViewModel.Text`, backing the hosted `TextEditor`'s
displayed content, exactly matches the contents of the opened temp file. Verified by
`SourceTextDocumentViewModelTests.Text_OpenedFile_MatchesFileContents`.
