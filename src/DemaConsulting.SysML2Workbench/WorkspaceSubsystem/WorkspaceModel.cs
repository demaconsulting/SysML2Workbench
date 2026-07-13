using DemaConsulting.SysML2Tools.Io;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Stdlib;

namespace DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

/// <summary>
///     Per-file load state tracked as part of an open workspace.
/// </summary>
/// <param name="FilePath">Absolute, normalized path of the tracked file.</param>
/// <param name="Diagnostics">Parser and semantic diagnostics attributed to this file.</param>
/// <param name="LoadedUtc">Timestamp of the load pass that produced this state.</param>
public sealed record WorkspaceFileState(
    string FilePath,
    IReadOnlyList<SysmlDiagnostic> Diagnostics,
    DateTimeOffset LoadedUtc);

/// <summary>
///     Options controlling how <see cref="WorkspaceModel" /> discovers and loads workspace files.
/// </summary>
/// <param name="GlobPatterns">Glob patterns (relative to the workspace root) selecting candidate model files.</param>
/// <param name="FileExtensions">
///     Extensions considered when a glob pattern's final segment is a bare wildcard. Ignored by patterns
///     that already name an extension (for example <c>**/*.sysml</c>).
/// </param>
public sealed record WorkspaceLoadOptions(IReadOnlyList<string> GlobPatterns, IReadOnlyList<string> FileExtensions)
{
    /// <summary>
    ///     Default options matching the SysML2Tools CLI's own default: every <c>.sysml</c> file anywhere
    ///     under the workspace root.
    /// </summary>
    public static WorkspaceLoadOptions Default { get; } = new(["**/*.sysml"], []);
}

/// <summary>
///     Immutable snapshot of a loaded workspace at a point in time.
/// </summary>
/// <param name="RootPath">Absolute workspace root folder the snapshot was loaded from.</param>
/// <param name="Files">Discovered files that were combined into the workspace.</param>
/// <param name="Workspace">Semantic workspace produced by SysML2Tools for the discovered files.</param>
/// <param name="Diagnostics">All parser and semantic diagnostics produced by the load.</param>
/// <param name="RevisionId">Opaque token that changes every time a new snapshot is published.</param>
public sealed record WorkspaceSnapshot(
    string RootPath,
    IReadOnlyList<string> Files,
    SysmlWorkspace Workspace,
    IReadOnlyList<SysmlDiagnostic> Diagnostics,
    string RevisionId);

/// <summary>
///     WorkspaceModel is the authoritative in-memory representation of the opened workspace, including
///     discovered files, resolved imports, and the semantic state needed by view selection, rendering, and
///     diagnostics.
/// </summary>
/// <remarks>
///     Deviation from the original design sketch: the drafted "ImportGraph" data item (a file-to-file import
///     dependency map) is not implemented. The real DemaConsulting.SysML2Tools semantic model
///     (<see cref="DemaConsulting.SysML2Tools.Semantic.Model.SysmlNode" /> and its subclasses) does not expose a
///     file-membership or per-file import property - cross-file references resolve against the combined
///     multi-file symbol table built from the whole discovered file set, not through individually tracked file
///     dependency edges. <see cref="ReloadFilesAsync" /> therefore performs a full workspace re-load rather than
///     a dependency-scoped partial reload; per-file state entries whose diagnostics are unchanged after a reload
///     keep their original <see cref="WorkspaceFileState.LoadedUtc" /> value so callers can still tell which
///     files were actually affected by the last reload.
/// </remarks>
public sealed class WorkspaceModel
{
    /// <summary>
    ///     Per-file load state for the currently loaded workspace, keyed by absolute file path.
    /// </summary>
    private readonly Dictionary<string, WorkspaceFileState> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Semantic workspace produced by the most recent load, or <see langword="null" /> before the first load.
    /// </summary>
    private SysmlWorkspace? _workspace;

    /// <summary>
    ///     Absolute path to the workspace folder currently loaded, or <see langword="null" /> before the first
    ///     load.
    /// </summary>
    public string? RootPath { get; private set; }

    /// <summary>
    ///     Maps normalized file paths to the latest known parse result and file metadata.
    /// </summary>
    public IReadOnlyDictionary<string, WorkspaceFileState> Files => _files;

    /// <summary>
    ///     Glob patterns, standard library inclusion, and reload behavior that remain consistent for the life of
    ///     the loaded workspace.
    /// </summary>
    public WorkspaceLoadOptions LoadOptions { get; private set; } = WorkspaceLoadOptions.Default;

    /// <summary>
    ///     Creates a workspace snapshot from a root folder.
    /// </summary>
    /// <remarks>
    ///     Discovers candidate files via <see cref="GlobFileCollector" />, loads the SysML standard library
    ///     symbols needed by the parser pipeline, parses and resolves imports across the discovered file set via
    ///     <see cref="WorkspaceLoader" />, and records diagnostics per file before publishing the initial
    ///     snapshot.
    /// </remarks>
    /// <param name="rootPath">Folder to scan for <c>.sysml</c> content.</param>
    /// <param name="options">Discovery and load options. Defaults to <see cref="WorkspaceLoadOptions.Default" />.</param>
    /// <returns>Normalized state for the full discovered workspace.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rootPath" /> is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="rootPath" /> does not exist.</exception>
    public async Task<WorkspaceSnapshot> LoadWorkspaceAsync(string rootPath, WorkspaceLoadOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Workspace root folder was not found: {rootPath}");
        }

        // Normalize the root path once so all subsequent file-path comparisons are consistent
        RootPath = Path.GetFullPath(rootPath);
        LoadOptions = options ?? WorkspaceLoadOptions.Default;
        _files.Clear();

        var snapshot = await LoadInternalAsync(RootPath, LoadOptions).ConfigureAwait(false);
        _workspace = snapshot.Workspace;
        return snapshot;
    }

    /// <summary>
    ///     Incrementally refreshes a subset of files.
    /// </summary>
    /// <remarks>
    ///     See the remarks on <see cref="WorkspaceModel" /> for why this recomputes the whole workspace rather
    ///     than a dependency-scoped subset: the real semantic model does not expose per-file import edges, so a
    ///     coherent reload requires reprocessing the full discovered file set. Only file entries whose
    ///     diagnostics actually changed receive a fresh <see cref="WorkspaceFileState.LoadedUtc" />.
    /// </remarks>
    /// <param name="changedPaths">Normalized files affected by an external change.</param>
    /// <returns>Updated workspace state after recomputation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changedPaths" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no workspace has been loaded yet.</exception>
    public async Task<WorkspaceSnapshot> ReloadFilesAsync(IReadOnlyList<string> changedPaths)
    {
        ArgumentNullException.ThrowIfNull(changedPaths);
        if (RootPath is null)
        {
            throw new InvalidOperationException("A workspace must be loaded before it can be reloaded.");
        }

        var snapshot = await LoadInternalAsync(RootPath, LoadOptions).ConfigureAwait(false);
        _workspace = snapshot.Workspace;
        return snapshot;
    }

    /// <summary>
    ///     Returns the current semantic view for downstream consumers.
    /// </summary>
    /// <returns>The workspace representation consumed by view and layout operations.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace has been loaded yet.</exception>
    public SysmlWorkspace GetSemanticWorkspace()
    {
        if (_workspace is null)
        {
            throw new InvalidOperationException("A workspace must be loaded before it can be retrieved.");
        }

        return _workspace;
    }

    /// <summary>
    ///     Discovers, parses, and semantically resolves the workspace files rooted at <paramref name="rootPath" />,
    ///     then merges the resulting per-file diagnostics into <see cref="_files" />.
    /// </summary>
    /// <param name="rootPath">Absolute workspace root folder.</param>
    /// <param name="options">Discovery and load options to apply.</param>
    /// <returns>The freshly computed workspace snapshot.</returns>
    private async Task<WorkspaceSnapshot> LoadInternalAsync(string rootPath, WorkspaceLoadOptions options)
    {
        // Mirror the SysML2Tools CLI's own multi-file discovery so imports across files resolve the same way
        var discoveredFiles = GlobFileCollector.Collect(options.GlobPatterns, options.FileExtensions, rootPath);

        // The standard library symbol table is required so references into the SysML standard packages resolve
        var (symbolTable, _) = StdlibProvider.GetSymbolTable();

        var loadResult = await WorkspaceLoader.LoadAsync(discoveredFiles, symbolTable).ConfigureAwait(false);

        // Group the flat workspace diagnostic list by file so per-file state can be tracked and compared
        var diagnosticsByFile = loadResult.Diagnostics
            .GroupBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SysmlDiagnostic>)g.ToList(), StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var updatedFiles = new Dictionary<string, WorkspaceFileState>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in discoveredFiles)
        {
            var diagnostics = diagnosticsByFile.TryGetValue(path, out var fileDiagnostics)
                ? fileDiagnostics
                : [];

            // Preserve the previous state instance (and its LoadedUtc) when nothing actually changed for this
            // file, so callers can tell which files were truly affected by this reload pass
            updatedFiles[path] = _files.TryGetValue(path, out var previous) && previous.Diagnostics.SequenceEqual(diagnostics)
                ? previous
                : new WorkspaceFileState(path, diagnostics, now);
        }

        _files.Clear();
        foreach (var (path, state) in updatedFiles)
        {
            _files[path] = state;
        }

        return new WorkspaceSnapshot(rootPath, discoveredFiles, loadResult.Workspace!, loadResult.Diagnostics, Guid.NewGuid().ToString("N"));
    }
}
