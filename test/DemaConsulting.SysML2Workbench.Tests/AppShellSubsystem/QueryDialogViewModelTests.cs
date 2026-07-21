using DemaConsulting.SysML2Tools.Query;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="QueryDialogViewModel" />: the single form's Query-Type-driven auto
///     recompute contract (List's purely-client-side result, the ten element-scoped verbs' dispatch
///     through <see cref="QueryEngine" />), verb-specific option construction, and graceful reporting of
///     every recoverable failure mode through <see cref="QueryDialogViewModel.StatusMessage" /> - all
///     without any explicit "Run" method call, since the redesign recomputes on every relevant property
///     change.
/// </summary>
public sealed class QueryDialogViewModelTests : IDisposable
{
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-logs-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        if (Directory.Exists(_tempLogRoot))
        {
            Directory.Delete(_tempLogRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Writes a small sample workspace with a package containing a part def, a part usage, and a
    ///     nested package - enough distinct kinds for the picker and every element-scoped verb.
    /// </summary>
    private async Task WriteSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part engineInstance : Engine;\n"
            + "    package Nested;\n"
            + "}\n",
            TestContext.Current.CancellationToken);
    }

    private MainWindowShell CreateShell()
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Fake clipboard service capturing writes in-memory so <see cref="QueryDialogViewModel.CopyResultAsMarkdownAsync" />
    ///     and <see cref="QueryDialogViewModel.CopyResultAsJsonAsync" /> can be exercised without any live
    ///     Avalonia / OS clipboard - the same seam pattern the existing <c>DiagramDocumentViewModelTests</c>
    ///     uses.
    /// </summary>
    private sealed class FakeClipboardService : IClipboardService
    {
        public string? LastText { get; private set; }

        public Task SetTextAsync(string text)
        {
            LastText = text;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Validates the initial dialog state over an empty shell (no workspace open): no candidates in
    ///     the picker, <see cref="QueryDialogViewModel.IsWorkspaceEmpty" /> is <see langword="true" />,
    ///     and the default Query Type (<see cref="QueryVerb.List" />) still produces a (empty) client-side
    ///     result rather than <see langword="null" />.
    /// </summary>
    [Fact]
    public void Construction_EmptyShell_ReportsWorkspaceEmpty()
    {
        // Arrange
        using var shell = CreateShell();

        // Act
        var viewModel = new QueryDialogViewModel(shell);

        // Assert
        Assert.True(viewModel.IsWorkspaceEmpty);
        Assert.Equal(QueryVerb.List, viewModel.SelectedQueryType);
        Assert.Empty(viewModel.FilterOnly.DisplayedItems);
        Assert.NotNull(viewModel.CurrentResult); // BuildListResult fires unconditionally
        Assert.Equal("list", viewModel.CurrentResult!.Verb);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that both <see cref="QueryDialogViewModel.FilterOnly" /> and
    ///     <see cref="QueryDialogViewModel.Picker" /> populate from a loaded workspace, exclude stdlib
    ///     names by default, and start with no default type-filter chip (so every candidate shows
    ///     immediately) - both instances share the same candidate list from
    ///     <see cref="QueryDialogViewModel.RefreshFromWorkspace" />.
    /// </summary>
    [Fact]
    public async Task Construction_LoadedWorkspace_PopulatesSinglePicker()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        // Act
        var viewModel = new QueryDialogViewModel(shell);

        // Assert
        Assert.False(viewModel.IsWorkspaceEmpty);
        Assert.False(viewModel.IncludeStdlib);
        Assert.Empty(viewModel.Picker.ActiveTypeFilters);
        Assert.Contains("Sample::Engine", viewModel.Picker.DisplayedItems);
        Assert.Contains("Sample::engineInstance", viewModel.Picker.DisplayedItems);
        Assert.Empty(viewModel.FilterOnly.ActiveTypeFilters);
        Assert.Contains("Sample::Engine", viewModel.FilterOnly.DisplayedItems);
        Assert.Contains("Sample::engineInstance", viewModel.FilterOnly.DisplayedItems);
    }

    /// <summary>
    ///     Validates that the "List" Query Type's <see cref="QueryDialogViewModel.CurrentResult" /> is a
    ///     client-built <see cref="QueryResult" /> with <see cref="QueryResult.Verb" /> equal to
    ///     <c>"list"</c>, no <see cref="QueryResult.Element" />, a count-summary line, and one entry per
    ///     displayed picker item (kind label sourced from the same candidate map).
    /// </summary>
    [Fact]
    public async Task ListQueryType_BuildsClientSideListResult()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);

        // Act
        var result = viewModel.CurrentResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("list", result!.Verb);
        Assert.Null(result.Element);
        Assert.Single(result.Summary);
        Assert.Contains("element(s) match the filter", result.Summary[0]);
        Assert.Equal(viewModel.FilterOnly.DisplayedItems.Count, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.QualifiedName == "Sample::Engine" && e.Kind == "part def");
        Assert.Contains(result.Entries, e => e.QualifiedName == "Sample::engineInstance" && e.Kind == "part");
    }

    /// <summary>
    ///     Validates that as the picker narrows (via a search text edit here) while "List" is selected,
    ///     the result regenerates automatically - the redesign's "live, no Run gesture" contract.
    /// </summary>
    [Fact]
    public async Task ListQueryType_SearchTextEdit_RegeneratesResultLive()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);

        // Act
        viewModel.FilterOnly.SearchText = "engineInstance";

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Single(viewModel.CurrentResult!.Entries);
        Assert.Equal("Sample::engineInstance", viewModel.CurrentResult.Entries[0].QualifiedName);
    }

    /// <summary>
    ///     Validates that selecting an element-scoped Query Type with no picker selection reports a
    ///     helpful (non-error) prompt naming the Query Type, and clears any stale prior result rather
    ///     than leaving it on screen.
    /// </summary>
    [Fact]
    public async Task RecomputeResult_ElementScopedVerbNoSelection_ReportsPromptAndClearsRows()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);

        // Act: switch to Describe with no selection on the picker
        viewModel.SelectedQueryType = QueryVerb.Describe;

        // Assert
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("Select an element above", viewModel.StatusMessage);
        Assert.Contains("Describe", viewModel.StatusMessage);
        Assert.Null(viewModel.CurrentResult);
        Assert.Empty(viewModel.CurrentResultRows);
    }

    /// <summary>
    ///     Validates that selecting an element-scoped Query Type over an empty workspace stops early with
    ///     a user-visible status, without touching <see cref="QueryEngine" />.
    /// </summary>
    [Fact]
    public void RecomputeResult_EmptyWorkspace_ReportsStatusMessage()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);

        // Act
        viewModel.SelectedQueryType = QueryVerb.Describe;

        // Assert
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("No workspace", viewModel.StatusMessage);
        Assert.Null(viewModel.CurrentResult);
    }

    /// <summary>
    ///     Validates that selecting Describe and then setting the picker's selected qualified name
    ///     dispatches through <see cref="QueryEngine.Execute" /> immediately, with no intervening method
    ///     call - proving the auto-recompute contract at the heart of this redesign.
    /// </summary>
    [Fact]
    public async Task RecomputeResult_DescribeWithSelection_DispatchesThroughEngineImmediately()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Describe;

        // Act: the assignment itself is the trigger under test - no Run call exists in this design.
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("describe", viewModel.CurrentResult!.Verb);
        Assert.Equal("Sample::Engine", viewModel.CurrentResult.Element);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that switching from Describe (with a selection) to List immediately shows the
    ///     client-side list result, proving Query Type switches recompute without stale state from the
    ///     previous verb.
    /// </summary>
    [Fact]
    public async Task SwitchingQueryType_FromDescribeToList_ShowsListResultImmediately()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Describe;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";
        Assert.Equal("describe", viewModel.CurrentResult!.Verb);

        // Act
        viewModel.SelectedQueryType = QueryVerb.List;

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("list", viewModel.CurrentResult!.Verb);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that switching from the default List Query Type (which always has a result) to
    ///     Describe with no selection shows the "select an element" prompt rather than a stale List
    ///     result or a thrown exception.
    /// </summary>
    [Fact]
    public async Task SwitchingQueryType_FromListToDescribeNoSelection_ShowsSelectPrompt()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        Assert.NotNull(viewModel.CurrentResult); // List's default result

        // Act
        viewModel.SelectedQueryType = QueryVerb.Describe;

        // Assert
        Assert.Null(viewModel.CurrentResult);
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("Select an element above", viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that changing <see cref="QueryDialogViewModel.HierarchyDirection" /> while
    ///     <see cref="QueryVerb.Hierarchy" /> is selected with an active selection recomputes immediately,
    ///     without requiring any manual call.
    /// </summary>
    [Fact]
    public async Task HierarchyDirectionChange_WithSelection_RecomputesImmediately()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Hierarchy;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";
        Assert.NotNull(viewModel.CurrentResult);

        // Act
        viewModel.HierarchyDirection = "up";

        // Assert: recomputed live with no stale state or thrown exception
        Assert.Null(viewModel.StatusMessage);
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("hierarchy", viewModel.CurrentResult!.Verb);
    }

    /// <summary>
    ///     Validates that editing <see cref="QueryDialogViewModel.WalkDepthText" /> while
    ///     <see cref="QueryVerb.Impact" /> is selected with an active selection recomputes immediately.
    /// </summary>
    [Fact]
    public async Task WalkDepthTextChange_WithSelection_RecomputesImmediately()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Impact;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";
        Assert.NotNull(viewModel.CurrentResult);

        // Act
        viewModel.WalkDepthText = "1";

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("impact", viewModel.CurrentResult!.Verb);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.BuildOptions" /> only attaches
    ///     <see cref="QueryOptions.Direction" /> when the current Query Type is
    ///     <see cref="QueryVerb.Hierarchy" />, matching the plan's per-verb visibility rules.
    /// </summary>
    [Fact]
    public void BuildOptions_HierarchyVerb_AttachesDirection()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Hierarchy;
        viewModel.HierarchyDirection = "up";

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.Equal(QueryVerb.Hierarchy, options.Verb);
        Assert.Equal("Some::Element", options.Element);
        Assert.Equal("up", options.Direction);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.BuildOptions" /> omits
    ///     <see cref="QueryOptions.Direction" /> for every non-Hierarchy verb, even when
    ///     <see cref="QueryDialogViewModel.HierarchyDirection" /> is set (the field simply doesn't apply).
    /// </summary>
    [Fact]
    public void BuildOptions_NonHierarchyVerb_OmitsDirection()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Describe;
        viewModel.HierarchyDirection = "up";

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.Null(options.Direction);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.BuildOptions" /> parses
    ///     <see cref="QueryDialogViewModel.WalkDepthText" /> as an integer only for
    ///     <see cref="QueryVerb.Impact" />, and only when it parses cleanly.
    /// </summary>
    [Fact]
    public void BuildOptions_ImpactVerbWithWalkDepth_ParsesWalkDepth()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Impact;
        viewModel.WalkDepthText = "3";

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.Equal(QueryVerb.Impact, options.Verb);
        Assert.Equal(3, options.WalkDepth);
    }

    /// <summary>
    ///     Validates that non-numeric or blank <see cref="QueryDialogViewModel.WalkDepthText" /> leaves
    ///     <see cref="QueryOptions.WalkDepth" /> null (the CLI's "unlimited" default), rather than
    ///     throwing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-a-number")]
    [InlineData("-1")]
    public void BuildOptions_ImpactVerbWithInvalidWalkDepth_LeavesNull(string? walkDepthText)
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Impact;
        viewModel.WalkDepthText = walkDepthText;

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.Null(options.WalkDepth);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.IncludeStdlib" /> flows through into
    ///     <see cref="QueryOptions.IncludeStdlib" /> for every verb.
    /// </summary>
    [Fact]
    public void BuildOptions_PropagatesIncludeStdlib()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.IncludeStdlib = true;
        viewModel.SelectedQueryType = QueryVerb.Uses;

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.True(options.IncludeStdlib);
    }

    /// <summary>
    ///     Validates that toggling <see cref="QueryDialogViewModel.IncludeStdlib" /> recomputes both the
    ///     picker's candidate set (added/removed stdlib names) and the current result. Because
    ///     <see cref="ElementPickerViewModel.SetCandidates" /> unconditionally clears
    ///     <see cref="ElementPickerViewModel.SelectedQualifiedName" /> on every refresh (so a stale prior
    ///     selection can never linger past a workspace-derived refresh), the immediately-following
    ///     recompute correctly drops the Describe-tab's prior result and shows the "select an element"
    ///     prompt instead of silently leaving the old (now unselected) result on screen - proving the
    ///     toggle live-recomputes rather than leaving a stale <see cref="QueryResult" /> from before the
    ///     toggle. Re-selecting after the toggle then confirms the whole
    ///     refresh-candidates-then-recompute pipeline still functions end to end.
    /// </summary>
    [Fact]
    public async Task IncludeStdlibToggle_RefreshesPickerAndRecomputesResult()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        var initialCount = viewModel.Picker.DisplayedItems.Count;

        viewModel.SelectedQueryType = QueryVerb.Describe;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";
        Assert.NotNull(viewModel.CurrentResult);

        // Act - enable stdlib (adds the stdlib names); SetCandidates clears the prior selection, so the
        // recompute correctly drops the stale Describe result rather than leaving it in place.
        viewModel.IncludeStdlib = true;
        var withStdlibCount = viewModel.Picker.DisplayedItems.Count;

        // Assert: candidate set grew (or stayed the same, if the sample workspace has no stdlib
        // declarations reachable), and the result was actively recomputed to the no-selection prompt
        // rather than left stale.
        Assert.True(withStdlibCount >= initialCount);
        Assert.Null(viewModel.CurrentResult);
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("Select an element above", viewModel.StatusMessage);

        // Act - reselect the element post-toggle, confirming the pipeline still recomputes correctly.
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("describe", viewModel.CurrentResult!.Verb);

        // Act - toggle back
        viewModel.IncludeStdlib = false;

        // Assert
        Assert.Equal(initialCount, viewModel.Picker.DisplayedItems.Count);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.CopyResultAsMarkdownAsync" /> writes the same
    ///     text <see cref="QueryResultRenderer.RenderMarkdown" /> produces (joined with newlines) into
    ///     the injected clipboard service.
    /// </summary>
    [Fact]
    public async Task CopyResultAsMarkdownAsync_WritesRenderedMarkdownToClipboard()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;
        viewModel.SelectedQueryType = QueryVerb.Describe;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";

        // Act
        await viewModel.CopyResultAsMarkdownAsync();

        // Assert
        Assert.NotNull(clipboard.LastText);
        var expected = string.Join("\n", QueryResultRenderer.RenderMarkdown(viewModel.CurrentResult!));
        Assert.Equal(expected, clipboard.LastText);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.CopyResultAsJsonAsync" /> writes the same
    ///     text <see cref="QueryResultRenderer.RenderJson" /> produces into the injected clipboard
    ///     service.
    /// </summary>
    [Fact]
    public async Task CopyResultAsJsonAsync_WritesRenderedJsonToClipboard()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;
        viewModel.SelectedQueryType = QueryVerb.Describe;
        viewModel.Picker.SelectedQualifiedName = "Sample::Engine";

        // Act
        await viewModel.CopyResultAsJsonAsync();

        // Assert
        Assert.NotNull(clipboard.LastText);
        Assert.Equal(QueryResultRenderer.RenderJson(viewModel.CurrentResult!), clipboard.LastText);
    }

    /// <summary>
    ///     Validates that the copy methods are a no-op (no exception, no clipboard write) when
    ///     <see cref="QueryDialogViewModel.CurrentResult" /> is <see langword="null" /> - exercised here
    ///     via the natural no-selection path (Describe with no picker selection) rather than a reflection
    ///     hack, which doubles as coverage for that path.
    /// </summary>
    [Fact]
    public async Task CopyMethods_NoResult_AreNoOps()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedQueryType = QueryVerb.Describe;
        Assert.Null(viewModel.CurrentResult);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;

        // Act
        await viewModel.CopyResultAsMarkdownAsync();
        await viewModel.CopyResultAsJsonAsync();

        // Assert
        Assert.Null(clipboard.LastText);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.QueryTypes" /> exposes exactly the eleven
    ///     user-facing Query Type options, with <see cref="QueryVerb.List" /> first, and never includes
    ///     <see cref="QueryVerb.Find" /> (which "List" always merges into, so the user never sees it).
    /// </summary>
    [Fact]
    public void QueryTypes_HasExpectedElevenEntries()
    {
        // Assert
        Assert.Equal(11, QueryDialogViewModel.QueryTypes.Count);
        Assert.Equal(QueryVerb.List, QueryDialogViewModel.QueryTypes[0]);
        Assert.DoesNotContain(QueryVerb.Find, QueryDialogViewModel.QueryTypes);
        Assert.Contains(QueryVerb.Describe, QueryDialogViewModel.QueryTypes);
        Assert.Contains(QueryVerb.Hierarchy, QueryDialogViewModel.QueryTypes);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.HierarchyDirectionOptions" /> covers the three
    ///     hierarchy directions accepted by the underlying <see cref="QueryOptions.Direction" /> field.
    /// </summary>
    [Fact]
    public void HierarchyDirectionOptions_HasExpectedThree()
    {
        // Assert
        Assert.Equal(new[] { "up", "down", "both" }, QueryDialogViewModel.HierarchyDirectionOptions);
    }
}
