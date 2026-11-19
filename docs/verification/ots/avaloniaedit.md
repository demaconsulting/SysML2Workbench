## AvaloniaEdit

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/AvaloniaEditTests.cs` verify AvaloniaEdit's read-only enforcement
and highlighting-load behavior directly: `[AvaloniaFact]` tests host a real, headless `AvaloniaEdit.TextEditor` to
confirm its `IsReadOnly` mode installs a section provider that genuinely rejects insertion, and that the real
`SourceTextDocumentView` loads the embedded `Assets/SysML.xshd` highlighting definition and highlights genuine SysML v2
keywords - including one absent from its hard-coded fallback list - with the Keyword color. Unit tests in
`test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/SourceTextDocumentViewModelTests.cs` additionally
verify that a file's text is faithfully surfaced, unmodified, to the editor's view model.

### Test Scenarios

**TextEditor_IsReadOnly_PreventsEdits**: A real, headless-hosted `TextEditor` with `IsReadOnly = true` installs a
`TextArea.ReadOnlySectionProvider` whose `CanInsert` rejects insertion anywhere in the document, while a normal
(non-read-only) editor's provider allows it - proving the read-only "View Source" tab genuinely cannot be edited
through the editor's own input pipeline.

**EmbeddedXshd_Loads_AndKeywordRuleMatchesSysmlKeyword**: Constructing the real `SourceTextDocumentView` assigns its
hosted editor the registered "SysML" highlighting definition loaded from the embedded `Assets/SysML.xshd` resource,
with the expected named colors (Keyword, Comment, String, Number). Highlighting real SysML v2 text with AvaloniaEdit's
`DocumentHighlighter` colors both `part` and `occurrence` with the Keyword color; `occurrence` is absent from the
view's hard-coded fallback list, so the keyword set is observably derived from the real SysML v2 lexer.

**Text_OpenedFile_MatchesFileContents**: `SourceTextDocumentViewModel.Text`, backing the hosted `TextEditor`'s
displayed content, exactly matches the contents of the opened temp file. Verified by
`SourceTextDocumentViewModelTests.Text_OpenedFile_MatchesFileContents`.
