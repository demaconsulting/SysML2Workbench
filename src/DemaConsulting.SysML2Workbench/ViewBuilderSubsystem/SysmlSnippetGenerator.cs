using System.Text;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

namespace DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

/// <summary>
///     Formatting options for <see cref="SysmlSnippetGenerator" />.
/// </summary>
/// <param name="EmitExplicitName">
///     Whether to emit an explicit view name (using <see cref="ViewDefinitionModel.DisplayName" /> when set, or a
///     generated default otherwise). Always <see langword="true" /> in Phase 0, since a named
///     <c>view</c> usage is required for <c>expose</c> statements to be valid SysML v2 syntax.
/// </param>
public sealed record SnippetGenerationOptions(bool EmitExplicitName = true);

/// <summary>
///     SysmlSnippetGenerator turns the current custom-view definition into readable, copy-pasteable SysML source
///     so users can persist a GUI-authored view in a normal model file without introducing a second storage
///     format.
/// </summary>
public sealed class SysmlSnippetGenerator
{
    /// <summary>
    ///     Default view name used when the definition does not supply a <see cref="ViewDefinitionModel.DisplayName" />.
    /// </summary>
    private const string DefaultViewName = "CustomView";

    /// <summary>
    ///     Identifiers that are reserved SysML v2 keywords and must be quoted if used as a view name.
    /// </summary>
    private static readonly HashSet<string> ReservedWords = new(StringComparer.Ordinal)
    {
        "abstract", "action", "allocate", "attribute", "bind", "comment", "connect", "connection", "def", "do",
        "doc", "else", "entry", "exhibit", "exit", "expose", "filter", "first", "flow", "if", "import", "include",
        "individual", "item", "metadata", "package", "part", "perform", "port", "private", "protected", "public",
        "render", "requirement", "satisfy", "send", "snapshot", "state", "succession", "terminate", "then",
        "timeslice", "transition", "variation", "verify", "view",
    };

    /// <summary>
    ///     Whitespace prefix used when emitting nested SysML clauses.
    /// </summary>
    public string Indentation { get; init; } = "    ";

    /// <summary>
    ///     Line separator applied consistently to generated text.
    /// </summary>
    public string LineEnding { get; init; } = "\n";

    /// <summary>
    ///     Identifier rewrite rules used when a generated name would otherwise collide with syntax. Exposed for
    ///     inspection; <see cref="SanitizeIdentifier" /> is the unit that actually applies escaping.
    /// </summary>
    public IReadOnlyDictionary<string, string> ReservedWordEscapes { get; } =
        ReservedWords.ToDictionary(word => word, word => $"'{word}'", StringComparer.Ordinal);

    /// <summary>
    ///     Produces SysML text for a custom view definition.
    /// </summary>
    /// <param name="definition">Normalized custom-view state. Must contain at least one expose target.</param>
    /// <param name="options">Formatting choices. Defaults to <see cref="SnippetGenerationOptions" /> defaults.</param>
    /// <returns>Complete SysML snippet ready for copy or save.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <paramref name="definition" /> has no view kind selected or no expose targets, since
    ///     emitting malformed SysML would violate this unit's purpose.
    /// </exception>
    public string GenerateSnippet(ViewDefinitionModel definition, SnippetGenerationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.ViewKind is null)
        {
            throw new InvalidOperationException("A view kind must be selected before a snippet can be generated.");
        }

        if (definition.ExposeTargets.Count == 0)
        {
            throw new InvalidOperationException("At least one expose target is required before a snippet can be generated.");
        }

        options ??= new SnippetGenerationOptions();
        var name = options.EmitExplicitName
            ? SanitizeIdentifier(definition.DisplayName ?? DefaultViewName)
            : SanitizeIdentifier(DefaultViewName);

        var builder = new StringBuilder();
        builder.Append("view ").Append(name).Append(" {").Append(LineEnding);

        foreach (var target in definition.ExposeTargets)
        {
            builder.Append(FormatExposeClause(target)).Append(LineEnding);
        }

        builder.Append(Indentation).Append("render ").Append(definition.ViewKind.Value.ToRenderTargetName()).Append(';').Append(LineEnding);

        if (!string.IsNullOrWhiteSpace(definition.FilterExpression))
        {
            builder.Append(Indentation).Append("filter ").Append(definition.FilterExpression).Append(';').Append(LineEnding);
        }

        builder.Append('}').Append(LineEnding);
        return builder.ToString();
    }

    /// <summary>
    ///     Emits one <c>expose</c> statement.
    /// </summary>
    /// <param name="target">Selected package or element qualified name.</param>
    /// <returns>Formatted clause for the target.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is null or whitespace.</exception>
    public string FormatExposeClause(string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        // Qualified names are preserved verbatim: SysML2Tools resolves expose targets by qualified name, and
        // inventing a proprietary alias format here would violate this unit's documented purpose
        return $"{Indentation}expose {target};";
    }

    /// <summary>
    ///     Makes an optional exported view name safe for SysML text.
    /// </summary>
    /// <param name="rawName">Candidate name from the UI.</param>
    /// <returns>Safe identifier or single-quoted representation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rawName" /> is empty.</exception>
    public string SanitizeIdentifier(string rawName)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawName);

        var isPlainIdentifier = rawName.Length > 0
            && (char.IsLetter(rawName[0]) || rawName[0] == '_')
            && rawName.All(c => char.IsLetterOrDigit(c) || c == '_');

        if (isPlainIdentifier && !ReservedWords.Contains(rawName))
        {
            return rawName;
        }

        // SysML v2 supports arbitrary "unrestricted" names quoted with single quotes; embedded quotes and
        // backslashes are escaped so the result is always safe to embed
        var escaped = rawName.Replace("\\", "\\\\").Replace("'", "\\'");
        return $"'{escaped}'";
    }
}
