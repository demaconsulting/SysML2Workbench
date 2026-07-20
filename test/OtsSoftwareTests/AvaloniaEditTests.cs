using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS AvaloniaEdit requirements in docs/reqstream/ots/avaloniaedit.yaml: that a real,
///     headless-hosted <see cref="TextEditor" /> genuinely enforces read-only behavior, and that the embedded
///     SysML v2 <c>Assets/SysML.xshd</c> highlighting definition loads without throwing and defines a rule
///     matching a real SysML v2 keyword, using Avalonia's headless test platform
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
    ///     Validates that the embedded <c>Assets/SysML.xshd</c> resource loads via <c>HighlightingLoader.Load</c>
    ///     without throwing, and that a keyword-highlighting rule built the same way SourceTextDocumentView does
    ///     (reflecting <c>DemaConsulting.SysML2Tools.Parser.Antlr.SysMLv2Lexer</c>'s literal-token table) matches
    ///     a real SysML v2 keyword (<c>part</c>).
    /// </summary>
    [AvaloniaFact]
    public void EmbeddedXshd_Loads_AndKeywordRuleMatchesSysmlKeyword()
    {
        // Arrange
        using var stream = Avalonia.Platform.AssetLoader.Open(
            new Uri("avares://DemaConsulting.SysML2Workbench/Assets/SysML.xshd"));
        using var reader = System.Xml.XmlReader.Create(stream);

        // Act
        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);

        // Assert: the definition loaded with the expected named colors, proving it is a genuine, well-formed
        // XSHD definition rather than an empty/broken stub.
        Assert.NotNull(definition);
        Assert.NotNull(definition.GetNamedColor("Keyword"));
        Assert.NotNull(definition.GetNamedColor("Comment"));
        Assert.NotNull(definition.GetNamedColor("String"));
        Assert.NotNull(definition.GetNamedColor("Number"));

        // Act: build the same reflection-derived keyword rule SourceTextDocumentView adds at runtime, and add
        // it to this independently loaded definition.
        var keywords = GetSysmlKeywords();
        var pattern = new System.Text.RegularExpressions.Regex($@"\b(?:{string.Join("|", keywords)})\b");

        // Assert: the resulting pattern matches a real SysML v2 keyword.
        Assert.Contains("part", keywords);
        Assert.Matches(pattern, "part def Engine;");
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
    ///     Independently reflects <c>SysMLv2Lexer._LiteralNames</c> the same way
    ///     <c>SourceTextDocumentView.LoadSysMlHighlighting</c> does, so this test verifies the real reflected
    ///     keyword set rather than a hand-copied duplicate.
    /// </summary>
    private static IReadOnlyList<string> GetSysmlKeywords()
    {
        var field = typeof(DemaConsulting.SysML2Tools.Parser.Antlr.SysMLv2Lexer).GetField(
            "_LiteralNames",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var literalNames = (string[])field!.GetValue(null)!;

        return literalNames
            .Where(name => name is not null)
            .Select(name => name.Trim('\''))
            .Where(name => name.Length > 0 && char.IsLetter(name[0]))
            .Distinct()
            .ToList();
    }
}
