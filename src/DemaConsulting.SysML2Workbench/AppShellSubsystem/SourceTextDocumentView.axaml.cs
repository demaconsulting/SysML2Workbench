using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using DemaConsulting.SysML2Tools.Parser.Antlr;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for one open read-only source-text tab, hosting an AvaloniaEdit
///     <see cref="AvaloniaEdit.TextEditor" /> bound one-way to <see cref="SourceTextDocumentViewModel.Text" />.
///     Assigns the embedded SysML v2 <see cref="AvaloniaEdit.Highlighting.IHighlightingDefinition" /> in
///     code-behind, since loading and registering an XSHD definition requires code, not a pure XAML markup
///     extension.
/// </summary>
public partial class SourceTextDocumentView : UserControl
{
    /// <summary>
    ///     Lazily-initialized, shared <see cref="IHighlightingDefinition" /> loaded once from the embedded
    ///     <c>Assets/SysML.xshd</c> resource and reused by every open source-text tab, rather than reloading and
    ///     re-registering it per tab.
    /// </summary>
    private static readonly Lazy<IHighlightingDefinition> SysMlHighlighting = new(LoadSysMlHighlighting);

    /// <summary>
    ///     Constructor used both at runtime (by Dock's view locator) and by the Avalonia XAML previewer/designer.
    /// </summary>
    public SourceTextDocumentView()
    {
        InitializeComponent();

        SourceTextEditor.SyntaxHighlighting = SysMlHighlighting.Value;

        DataContextChanged += OnDataContextChanged;

        if (Design.IsDesignMode)
        {
            DataContext = new SourceTextDocumentViewModel(DesignTimeShellFactory.Create(), "design-preview");
        }
    }

    /// <summary>
    ///     Assigns the newly bound <see cref="SourceTextDocumentViewModel.Text" /> to the editor's document once,
    ///     when the view model is first attached - a one-way, one-time assignment (not a two-way XAML binding)
    ///     since <see cref="AvaloniaEdit.TextEditor.Text" /> is a plain CLR property, not an Avalonia styled
    ///     property, and this Phase 1 viewer's text never changes after construction anyway.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SourceTextDocumentViewModel viewModel)
        {
            SourceTextEditor.Text = viewModel.Text;
        }
    }

    /// <summary>
    ///     Loads the embedded SysML v2 XSHD highlighting definition and registers it with
    ///     <see cref="HighlightingManager.Instance" />, adding a keyword-highlighting rule derived by reflecting
    ///     <see cref="SysMLv2Lexer" />'s own generated literal-token table (falling back to a small hard-coded
    ///     keyword list if that reflection ever fails, so a future SysML2Tools release that renames/removes the
    ///     reflected field degrades to a static keyword list instead of throwing).
    /// </summary>
    /// <returns>The loaded, registered highlighting definition.</returns>
    private static IHighlightingDefinition LoadSysMlHighlighting()
    {
        using var stream = AssetLoader.Open(new Uri("avares://DemaConsulting.SysML2Workbench/Assets/SysML.xshd"));
        using var reader = XmlReader.Create(stream);
        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);

        var keywordColor = definition.GetNamedColor("Keyword");
        var keywordPattern = $@"\b(?:{string.Join("|", GetKeywords())})\b";
        definition.MainRuleSet.Rules.Add(new HighlightingRule
        {
            Regex = new Regex(keywordPattern),
            Color = keywordColor,
        });

        HighlightingManager.Instance.RegisterHighlighting("SysML", [".sysml"], definition);
        return definition;
    }

    /// <summary>
    ///     Reflects <see cref="SysMLv2Lexer" />'s private static <c>_LiteralNames</c> field to derive the
    ///     authoritative set of SysML v2 keyword literals, so the highlighting rule's keyword list is never a
    ///     hand-copied duplicate of the grammar. Falls back to a small hard-coded keyword list if the reflected
    ///     field is ever renamed, removed, or otherwise inaccessible - reflecting a <c>private</c> field on a
    ///     third-party generated class is not a guaranteed-stable contract.
    /// </summary>
    /// <returns>The SysML v2 keyword literals.</returns>
    private static IReadOnlyList<string> GetKeywords()
    {
        try
        {
            var field = typeof(SysMLv2Lexer).GetField("_LiteralNames", BindingFlags.NonPublic | BindingFlags.Static);
            if (field?.GetValue(null) is string[] literalNames)
            {
                var keywords = literalNames
                    .Where(name => name is not null)
                    .Select(name => name!.Trim('\''))
                    .Where(name => name.Length > 0 && char.IsLetter(name[0]))
                    .Distinct()
                    .ToList();

                if (keywords.Count > 0)
                {
                    return keywords;
                }
            }
        }
        catch (Exception)
        {
            // Reflection against a private, generated field is inherently fragile against future
            // SysML2Tools upgrades - fall through to the hard-coded fallback list below.
        }

        return FallbackKeywords;
    }

    /// <summary>
    ///     Hard-coded fallback keyword list, used only if reflecting <see cref="SysMLv2Lexer" />'s literal-token
    ///     table ever fails. Kept intentionally small - a representative subset of the most common SysML v2
    ///     keywords - since this path exists purely so highlighting degrades gracefully rather than a
    ///     comprehensive, independently-maintained duplicate of the grammar.
    /// </summary>
    private static readonly string[] FallbackKeywords =
    [
        "package", "part", "def", "import", "view", "expose", "render", "attribute", "port", "connection",
        "interface", "action", "state", "transition", "requirement", "satisfy", "verify", "item", "flow",
        "connect", "to", "from", "public", "private", "protected", "abstract", "doc", "comment", "alias",
        "specialization", "specializes", "subsets", "redefines", "in", "out", "inout", "return", "if",
        "else", "then", "for", "while", "do", "true", "false", "null",
    ];
}
