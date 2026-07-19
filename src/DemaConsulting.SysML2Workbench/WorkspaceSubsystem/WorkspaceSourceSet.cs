using DemaConsulting.SysML2Tools.Io;

namespace DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

/// <summary>
///     Distinguishes whether a <see cref="WorkspaceSource" /> refers to a single file or a folder scanned for
///     matching model files.
/// </summary>
public enum WorkspaceSourceKind
{
    /// <summary>A single, explicitly added model file.</summary>
    File,

    /// <summary>A folder scanned for model files using <see cref="WorkspaceLoadOptions.Default" />.</summary>
    Folder,
}

/// <summary>
///     Options controlling how <see cref="WorkspaceSourceSet" /> discovers model files under a
///     <see cref="WorkspaceSourceKind.Folder" /> source.
/// </summary>
/// <param name="GlobPatterns">Glob patterns (relative to the folder source) selecting candidate model files.</param>
/// <param name="FileExtensions">
///     Extensions considered when a glob pattern's final segment is a bare wildcard. Ignored by patterns
///     that already name an extension (for example <c>**/*.sysml</c>).
/// </param>
public sealed record WorkspaceLoadOptions(IReadOnlyList<string> GlobPatterns, IReadOnlyList<string> FileExtensions)
{
    /// <summary>
    ///     Default options matching the SysML2Tools CLI's own default: every <c>.sysml</c> file anywhere
    ///     under the folder source. Applied to every <see cref="WorkspaceSourceKind.Folder" /> source - folder
    ///     discovery is not individually configurable per source.
    /// </summary>
    public static WorkspaceLoadOptions Default { get; } = new(["**/*.sysml"], []);
}

/// <summary>
///     One user-added workspace input: either a single file or a folder scanned for model files.
/// </summary>
/// <param name="Id">Stable identifier that survives reorder/refresh; used as the file-watcher key and UI tree node key.</param>
/// <param name="Kind">Whether this source is a single file or a scanned folder.</param>
/// <param name="Path">Absolute, normalized path (see <see cref="System.IO.Path.GetFullPath(string)" />).</param>
public sealed record WorkspaceSource(string Id, WorkspaceSourceKind Kind, string Path);

/// <summary>
///     The result of resolving every source in a <see cref="WorkspaceSourceSet" /> into a single, deduplicated
///     file list ready for <see cref="WorkspaceModel.LoadWorkspaceAsync" />.
/// </summary>
/// <param name="MergedFiles">Deduped union of every file discovered across all sources, in registration order.</param>
/// <param name="FileToSourceId">
///     Maps each merged file to the source that owns it. When the same file is reachable through more than one
///     source (for example a file explicitly added and also discovered under an overlapping folder), the
///     first-registered source wins attribution; this is a display-only tie-break and does not affect
///     <see cref="MergedFiles" />, which contains the file exactly once regardless.
/// </param>
/// <param name="SourceIdToFiles">
///     Maps each source id to the files it contributed: a <see cref="WorkspaceSourceKind.Folder" /> source maps
///     to every file discovered under it, and a <see cref="WorkspaceSourceKind.File" /> source maps to a
///     singleton list containing itself. Intended for tree-view consumers that need per-source grouping.
/// </param>
public sealed record WorkspaceSourceResolution(
    IReadOnlyList<string> MergedFiles,
    IReadOnlyDictionary<string, string> FileToSourceId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> SourceIdToFiles);

/// <summary>
///     WorkspaceSourceSet maintains the ordered set of file and folder sources a user has added to the workspace,
///     and resolves them into the merged, deduplicated file list consumed by <see cref="WorkspaceModel" />.
/// </summary>
public sealed class WorkspaceSourceSet
{
    /// <summary>
    ///     Ordered, mutable backing list for <see cref="Sources" />.
    /// </summary>
    private readonly List<WorkspaceSource> _sources = [];

    /// <summary>
    ///     Every source currently added, in the order they were registered.
    /// </summary>
    public IReadOnlyList<WorkspaceSource> Sources => _sources;

    /// <summary>
    ///     Adds a single file source, or returns the existing source if the same normalized path is already
    ///     registered as a <see cref="WorkspaceSourceKind.File" /> source.
    /// </summary>
    /// <param name="path">File path to add.</param>
    /// <returns>The newly created or already-existing source.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    public WorkspaceSource AddFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = Path.GetFullPath(path);
        var existing = _sources.FirstOrDefault(s =>
            s.Kind == WorkspaceSourceKind.File && string.Equals(s.Path, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var source = new WorkspaceSource(Guid.NewGuid().ToString("N"), WorkspaceSourceKind.File, normalized);
        _sources.Add(source);
        return source;
    }

    /// <summary>
    ///     Adds a folder source, or returns the existing source if the same normalized path is already registered
    ///     as a <see cref="WorkspaceSourceKind.Folder" /> source.
    /// </summary>
    /// <param name="path">Folder path to add.</param>
    /// <returns>The newly created or already-existing source.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="path" /> does not exist.</exception>
    public WorkspaceSource AddFolder(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Workspace folder was not found: {path}");
        }

        var normalized = Path.GetFullPath(path);
        var existing = _sources.FirstOrDefault(s =>
            s.Kind == WorkspaceSourceKind.Folder && string.Equals(s.Path, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var source = new WorkspaceSource(Guid.NewGuid().ToString("N"), WorkspaceSourceKind.Folder, normalized);
        _sources.Add(source);
        return source;
    }

    /// <summary>
    ///     Removes the source with the given identifier, if one is registered.
    /// </summary>
    /// <param name="sourceId">Identifier of the source to remove.</param>
    /// <returns><see langword="true" /> when a matching source was found and removed; otherwise <see langword="false" />.</returns>
    public bool RemoveSource(string sourceId)
    {
        var index = _sources.FindIndex(s => s.Id == sourceId);
        if (index < 0)
        {
            return false;
        }

        _sources.RemoveAt(index);
        return true;
    }

    /// <summary>
    ///     Resolves every registered source into a single, deduplicated file list plus per-file and per-source
    ///     attribution.
    /// </summary>
    /// <remarks>
    ///     Every <see cref="WorkspaceSourceKind.Folder" /> source is scanned with
    ///     <see cref="WorkspaceLoadOptions.Default" />. Overlapping files (a file explicitly added that also
    ///     falls under a folder source, or two overlapping folder sources) are silently deduplicated: the
    ///     first-registered source to reach a given normalized path wins attribution in
    ///     <see cref="WorkspaceSourceResolution.FileToSourceId" />. Zero sources resolves to an empty result with
    ///     no error.
    /// </remarks>
    /// <returns>The merged file list and its source attribution.</returns>
    public WorkspaceSourceResolution Resolve()
    {
        var mergedFiles = new List<string>();
        var fileToSourceId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceIdToFiles = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in _sources)
        {
            var options = WorkspaceLoadOptions.Default;
            IReadOnlyList<string> sourceFiles = source.Kind == WorkspaceSourceKind.Folder
                ? GlobFileCollector.Collect(options.GlobPatterns, options.FileExtensions, source.Path)
                : [source.Path];

            sourceIdToFiles[source.Id] = sourceFiles;

            foreach (var file in sourceFiles)
            {
                if (fileToSourceId.ContainsKey(file))
                {
                    // Already attributed to an earlier-registered source - dedupe silently, first source wins.
                    continue;
                }

                fileToSourceId[file] = source.Id;
                mergedFiles.Add(file);
            }
        }

        return new WorkspaceSourceResolution(mergedFiles, fileToSourceId, sourceIdToFiles);
    }
}
