using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS AvaloniaEdit requirements in docs/reqstream/ots/avaloniaedit.yaml: that a real,
///     headless-hosted <see cref="TextEditor" /> genuinely enforces read-only behavior, and that the real
///     <c>SourceTextDocumentView</c> loads the embedded SysML v2 <c>Assets/SysML.xshd</c> highlighting definition
///     and highlights real SysML v2 keywords, using Avalonia's headless test platform
///     (<see cref="AvaloniaFactAttribute" />) rather than a mocked or hand-rolled harness.
/// </summary>
public sealed class AvaloniaEditTests
{
    /// <summary>
    ///     Validates that a real, headless-hosted <see cref="TextEditor" /> with <c>IsReadOnly = true</c> installs
    ///     a <see cref="AvaloniaEdit.Editing.TextArea.ReadOnlySectionProvider" /> that rejects insertion anywhere
    ///     in the document, and that a normal (non-read-only) editor's provider allows it - proving
    ///     SysML2Workbench's read-only "View Source" tab genuinely cannot be edited through the editor's own
    ///     input pipeline (as opposed to only checking the document API, which read-only mode does not guard).
    /// </summary>
    [AvaloniaFact]
    public void TextEditor_IsReadOnly_PreventsEdits()
    {
        // Arrange
        var readOnlyEditor = new TextEditor
        {
            IsReadOnly = true,
            Text = "package Sample {}\n",
        };
        var editableEditor = new TextEditor
        {
            IsReadOnly = false,
            Text = "package Sample {}\n",
        };

        // Act
        var readOnlyCanInsert = readOnlyEditor.TextArea.ReadOnlySectionProvider.CanInsert(0);
        var editableCanInsert = editableEditor.TextArea.ReadOnlySectionProvider.CanInsert(0);

        // Assert: the read-only editor's section provider rejects insertion, while a normal editor's allows it.
        Assert.False(readOnlyCanInsert);
        Assert.True(editableCanInsert);
    }

    /// <summary>
    ///     Validates that the real <c>SourceTextDocumentView</c> loads the embedded <c>Assets/SysML.xshd</c>
    ///     resource through AvaloniaEdit's <c>HighlightingLoader</c>, registers it as the "SysML" definition, and
    ///     that the definition it assigns to its hosted editor highlights SysML v2 keywords with the Keyword color:
    ///     both a common keyword (<c>part</c>) and a keyword (<c>occurrence</c>) that is absent from the view's
    ///     small hard-coded fallback list, so the highlighted keyword set is observably derived from the real
    ///     SysML v2 lexer rather than a hand-copied list.
    /// </summary>
    [AvaloniaFact]
    public void EmbeddedXshd_Loads_AndKeywordRuleMatchesSysmlKeyword()
    {
        // Arrange: the production view, which loads and registers the embedded highlighting definition
        var view = new DemaConsulting.SysML2Workbench.AppShellSubsystem.SourceTextDocumentView();
        var editor = view.GetLogicalDescendants().OfType<TextEditor>().Single();

        // Act
        var definition = editor.SyntaxHighlighting;

        // Assert: the definition is the registered, well-formed XSHD definition with its expected named colors
        Assert.NotNull(definition);
        Assert.Same(definition, HighlightingManager.Instance.GetDefinition("SysML"));
        var keywordColor = definition.GetNamedColor("Keyword");
        Assert.NotNull(keywordColor);
        Assert.NotNull(definition.GetNamedColor("Comment"));
        Assert.NotNull(definition.GetNamedColor("String"));
        Assert.NotNull(definition.GetNamedColor("Number"));

        // Assert: highlighting real SysML v2 text colors both keywords with the Keyword color
        Assert.True(IsHighlightedAs(definition, "part def Engine;", "part", keywordColor));
        Assert.True(IsHighlightedAs(definition, "occurrence def Flight;", "occurrence", keywordColor));
    }

    /// <summary>
    ///     Validates that hosting a real <see cref="TextEditor" /> inside a real, shown <see cref="Window" /> under
    ///     this assembly's headless <c>App</c> (see <see cref="TestAppBuilder" />, which boots the real
    ///     <c>DemaConsulting.SysML2Workbench.App</c> composition root and its <c>App.axaml</c> styles) produces an
    ///     actual visual tree for the editor - guarding against the regression where <c>App.axaml</c> omitted
    ///     AvaloniaEdit's required <c>avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml</c> style include,
    ///     which left <see cref="TextEditor" /> with no control theme/template applied at all: the control's
    ///     <c>Text</c> property still read back correctly (so a data-binding-only test could not have caught it),
    ///     but nothing was ever actually drawn on screen. This test would have failed before that style include was
    ///     added, because a templated control with no theme applied has no realized visual children at all.
    /// </summary>
    [AvaloniaFact]
    public void TextEditor_HostedInRealWindow_GetsAControlThemeAndVisualTemplate()
    {
        // Arrange
        var editor = new TextEditor { Text = "package Sample {}\n", ShowLineNumbers = true };
        var window = new Window { Content = editor, Width = 400, Height = 300 };

        // Act
        window.Show();

        // Assert: a themed TemplatedControl always realizes at least one visual child from its control theme's
        // template once shown; a control with no theme applied (the pre-fix regression) has none.
        Assert.NotNull(editor.Template);
        Assert.True(editor.GetVisualChildren().Any());

        window.Close();
    }

    /// <summary>
    ///     Highlights a single line of text with <paramref name="definition" /> through AvaloniaEdit's own
    ///     <see cref="DocumentHighlighter" />, and reports whether the whole <paramref name="word" /> is covered by
    ///     a section colored <paramref name="expectedColor" />.
    /// </summary>
    private static bool IsHighlightedAs(IHighlightingDefinition definition, string text, string word, HighlightingColor expectedColor)
    {
        var document = new AvaloniaEdit.Document.TextDocument(text);
        var highlighter = new DocumentHighlighter(document, definition);
        var line = highlighter.HighlightLine(1);
        var start = text.IndexOf(word, StringComparison.Ordinal);

        return line.Sections.Any(section =>
            section.Offset <= start
            && section.Offset + section.Length >= start + word.Length
            && expectedColor.Equals(section.Color));
    }
}
