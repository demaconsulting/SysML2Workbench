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
///     Immutable snapshot of a loaded workspace at a point in time.
/// </summary>
/// <param name="Sources">Ordered file/folder sources the snapshot was resolved from. Empty for a first-class,
///     zero-source workspace.</param>
/// <param name="Files">Discovered files that were combined into the workspace.</param>
/// <param name="Workspace">Semantic workspace produced by SysML2Tools for the discovered files.</param>
/// <param name="Diagnostics">All parser and semantic diagnostics produced by the load.</param>
/// <param name="RevisionId">Opaque token that changes every time a new snapshot is published.</param>
public sealed record WorkspaceSnapshot(
    IReadOnlyList<WorkspaceSource> Sources,
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
///     files were actually affected by the last reload. File discovery itself is no longer this unit's concern:
///     <see cref="WorkspaceSourceSet" /> resolves the caller's file/folder sources into a merged file list, and
///     <see cref="WorkspaceModel" /> only ever loads the files it is handed.
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
    ///     Resolution used by the most recent load, defaulting to an empty resolution so
    ///     <see cref="ReloadFilesAsync" /> is well-defined even before the first <see cref="LoadWorkspaceAsync" />
    ///     call.
    /// </summary>
    private WorkspaceSourceResolution _resolution = new(
        [],
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    ///     Ordered file/folder sources currently loaded, or an empty list before the first load.
    /// </summary>
    public IReadOnlyList<WorkspaceSource> Sources { get; private set; } = [];

    /// <summary>
    ///     Maps normalized file paths to the latest known parse result and file metadata.
    /// </summary>
    public IReadOnlyDictionary<string, WorkspaceFileState> Files => _files;

    /// <summary>
    ///     Loads a workspace snapshot from an already-resolved set of sources.
    /// </summary>
    /// <remarks>
    ///     Loads the SysML standard library symbols needed by the parser pipeline, parses and resolves imports
    ///     across <paramref name="resolution" />'s merged file set via <see cref="WorkspaceLoader" />, and records
    ///     diagnostics per file before publishing the snapshot. A zero-source, zero-file resolution is a
    ///     first-class, valid input: <see cref="WorkspaceLoader.LoadAsync" /> called with an empty file list does
    ///     not throw and produces a valid, diagnostic-free, standard-library-only workspace.
    /// </remarks>
    /// <param name="sources">Ordered file/folder sources the resolution was computed from.</param>
    /// <param name="resolution">Already-resolved merged file set, typically from <see cref="WorkspaceSourceSet.Resolve" />.</param>
    /// <returns>Normalized state for the resolved workspace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources" /> or <paramref name="resolution" /> is null.</exception>
    public async Task<WorkspaceSnapshot> LoadWorkspaceAsync(IReadOnlyList<WorkspaceSource> sources, WorkspaceSourceResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(resolution);

        Sources = sources;
        _resolution = resolution;
        _files.Clear();

        var snapshot = await LoadInternalAsync(sources, resolution.MergedFiles).ConfigureAwait(false);
        _workspace = snapshot.Workspace;
        return snapshot;
    }

    /// <summary>
    ///     Incrementally refreshes a subset of files.
    /// </summary>
    /// <remarks>
    ///     See the remarks on <see cref="WorkspaceModel" /> for why this recomputes the whole workspace rather
    ///     than a dependency-scoped subset: the real semantic model does not expose per-file import edges, so a
    ///     coherent reload requires reprocessing the full resolved file set. Only file entries whose diagnostics
    ///     actually changed receive a fresh <see cref="WorkspaceFileState.LoadedUtc" />. Recomputes against the
    ///     current resolution even if it is empty (0 sources, 0 files) - this is a valid, non-throwing state.
    ///     When <paramref name="updatedResolution" /> is supplied, it replaces the stored resolution before
    ///     recomputation - needed so a file created or deleted externally under a still-registered folder source
    ///     (as opposed to a source being explicitly added/removed) is picked up: an external file-system change
    ///     does not go through <see cref="LoadWorkspaceAsync" />, so without a fresh resolution here, this method
    ///     would keep recomputing against the file list captured at the last explicit source-set mutation. When
    ///     omitted, the previously stored resolution is reused unchanged.
    /// </remarks>
    /// <param name="changedPaths">Normalized files affected by an external change.</param>
    /// <param name="updatedResolution">
    ///     Freshly recomputed resolution to adopt before reloading, or <see langword="null" /> to keep reloading
    ///     against the resolution from the most recent <see cref="LoadWorkspaceAsync" /> call.
    /// </param>
    /// <returns>Updated workspace state after recomputation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="changedPaths" /> is null.</exception>
    public async Task<WorkspaceSnapshot> ReloadFilesAsync(IReadOnlyList<string> changedPaths, WorkspaceSourceResolution? updatedResolution = null)
    {
        ArgumentNullException.ThrowIfNull(changedPaths);

        if (updatedResolution is not null)
        {
            _resolution = updatedResolution;
        }

        var snapshot = await LoadInternalAsync(Sources, _resolution.MergedFiles).ConfigureAwait(false);
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
    ///     Parses and semantically resolves <paramref name="discoveredFiles" />, then merges the resulting
    ///     per-file diagnostics into <see cref="_files" />.
    /// </summary>
    /// <param name="sources">Ordered file/folder sources the snapshot is published for.</param>
    /// <param name="discoveredFiles">Already-resolved, merged file set to load.</param>
    /// <returns>The freshly computed workspace snapshot.</returns>
    private async Task<WorkspaceSnapshot> LoadInternalAsync(IReadOnlyList<WorkspaceSource> sources, IReadOnlyList<string> discoveredFiles)
    {
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

        return new WorkspaceSnapshot(sources, discoveredFiles, loadResult.Workspace!, loadResult.Diagnostics, Guid.NewGuid().ToString("N"));
    }
}
