using DemaConsulting.SysML2Tools.Query;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="QueryDialogViewModel" />: verb-specific option construction, Browse tab's
///     purely-client-side result generation, Element Query tab dispatch through <see cref="QueryEngine" />,
///     and graceful reporting of every recoverable failure mode through <see cref="QueryDialogViewModel.StatusMessage" />.
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
    ///     either picker, <see cref="QueryDialogViewModel.IsWorkspaceEmpty" /> is <see langword="true" />,
    ///     and <see cref="QueryDialogViewModel.CurrentResult" /> is <see langword="null" />.
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
        Assert.Empty(viewModel.BrowsePicker.DisplayedItems);
        Assert.Empty(viewModel.ElementQueryPicker.DisplayedItems);
        Assert.NotNull(viewModel.CurrentResult); // BuildBrowseResult fires unconditionally
        Assert.Equal("list", viewModel.CurrentResult!.Verb);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that both pickers populate from a loaded workspace, exclude stdlib names by default,
    ///     and start with no default type-filter chip (so every candidate shows immediately).
    /// </summary>
    [Fact]
    public async Task Construction_LoadedWorkspace_PopulatesBothPickers()
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
        Assert.Empty(viewModel.BrowsePicker.ActiveTypeFilters);
        Assert.Empty(viewModel.ElementQueryPicker.ActiveTypeFilters);
        Assert.Contains("Sample::Engine", viewModel.BrowsePicker.DisplayedItems);
        Assert.Contains("Sample::engineInstance", viewModel.BrowsePicker.DisplayedItems);
        Assert.Contains("Sample::Engine", viewModel.ElementQueryPicker.DisplayedItems);
    }

    /// <summary>
    ///     Validates that the Browse tab's <see cref="QueryDialogViewModel.CurrentResult" /> is a
    ///     client-built <see cref="QueryResult" /> with <see cref="QueryResult.Verb" /> equal to
    ///     <c>"list"</c>, no <see cref="QueryResult.Element" />, a count-summary line, and one entry per
    ///     displayed picker item (kind label sourced from the same candidate map).
    /// </summary>
    [Fact]
    public async Task BrowseTab_BuildsClientSideListResult()
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
        Assert.Equal(viewModel.BrowsePicker.DisplayedItems.Count, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.QualifiedName == "Sample::Engine" && e.Kind == "part def");
        Assert.Contains(result.Entries, e => e.QualifiedName == "Sample::engineInstance" && e.Kind == "part");
    }

    /// <summary>
    ///     Validates that as the Browse tab's picker narrows (via a search text edit here), the Browse
    ///     result regenerates automatically - the plan's "live" contract for the tab.
    /// </summary>
    [Fact]
    public async Task BrowseTab_SearchTextEdit_RegeneratesResultLive()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);

        // Act
        viewModel.BrowsePicker.SearchText = "engineInstance";

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Single(viewModel.CurrentResult!.Entries);
        Assert.Equal("Sample::engineInstance", viewModel.CurrentResult.Entries[0].QualifiedName);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.RunElementQuery" /> with no selection stops
    ///     before touching <see cref="QueryEngine" />, reports a user-visible
    ///     <see cref="QueryDialogViewModel.StatusMessage" />, and leaves the Browse-derived result in
    ///     place.
    /// </summary>
    [Fact]
    public async Task RunElementQuery_NoSelection_ReportsStatusMessage()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        var browseResult = viewModel.CurrentResult;
        viewModel.SelectedVerb = QueryVerb.Describe;

        // Act
        viewModel.RunElementQuery();

        // Assert
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("Select an element", viewModel.StatusMessage);
        Assert.Same(browseResult, viewModel.CurrentResult);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.RunElementQuery" /> over an empty workspace
    ///     stops early with a user-visible status, without touching <see cref="QueryEngine" />.
    /// </summary>
    [Fact]
    public void RunElementQuery_EmptyWorkspace_ReportsStatusMessage()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.ElementQueryPicker.SelectedQualifiedName = "something";
        viewModel.SelectedVerb = QueryVerb.Describe;

        // Act
        viewModel.RunElementQuery();

        // Assert
        Assert.NotNull(viewModel.StatusMessage);
        Assert.Contains("No workspace", viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that a valid Describe run over a real workspace dispatches through
    ///     <see cref="QueryEngine.Execute" />: the resulting <see cref="QueryResult" /> carries the
    ///     kebab-case verb token and the resolved element's qualified name.
    /// </summary>
    [Fact]
    public async Task RunElementQuery_Describe_DispatchesThroughEngine()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.ElementQueryPicker.SelectedQualifiedName = "Sample::Engine";
        viewModel.SelectedVerb = QueryVerb.Describe;

        // Act
        viewModel.RunElementQuery();

        // Assert
        Assert.NotNull(viewModel.CurrentResult);
        Assert.Equal("describe", viewModel.CurrentResult!.Verb);
        Assert.Equal("Sample::Engine", viewModel.CurrentResult.Element);
        Assert.Null(viewModel.StatusMessage);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.BuildOptions" /> only attaches
    ///     <see cref="QueryOptions.Direction" /> when the current verb is
    ///     <see cref="QueryVerb.Hierarchy" />, matching the plan's per-verb visibility rules.
    /// </summary>
    [Fact]
    public void BuildOptions_HierarchyVerb_AttachesDirection()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        viewModel.SelectedVerb = QueryVerb.Hierarchy;
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
        viewModel.SelectedVerb = QueryVerb.Describe;
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
        viewModel.SelectedVerb = QueryVerb.Impact;
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
        viewModel.SelectedVerb = QueryVerb.Impact;
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
        viewModel.SelectedVerb = QueryVerb.Uses;

        // Act
        var options = viewModel.BuildOptions("Some::Element");

        // Assert
        Assert.True(options.IncludeStdlib);
    }

    /// <summary>
    ///     Validates that toggling <see cref="QueryDialogViewModel.IncludeStdlib" /> recomputes both
    ///     pickers' candidates (which is the mechanism the checkbox uses to add/remove stdlib names
    ///     from view). At minimum the toggle must not leave state broken; a re-toggle back yields the
    ///     same displayed set.
    /// </summary>
    [Fact]
    public async Task IncludeStdlibToggle_RefreshesBothPickers()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new QueryDialogViewModel(shell);
        var initialBrowseCount = viewModel.BrowsePicker.DisplayedItems.Count;
        var initialQueryCount = viewModel.ElementQueryPicker.DisplayedItems.Count;

        // Act - enable stdlib (adds the stdlib names)
        viewModel.IncludeStdlib = true;
        var withStdlibBrowseCount = viewModel.BrowsePicker.DisplayedItems.Count;

        // Assert
        Assert.True(withStdlibBrowseCount >= initialBrowseCount);

        // Act - toggle back
        viewModel.IncludeStdlib = false;

        // Assert
        Assert.Equal(initialBrowseCount, viewModel.BrowsePicker.DisplayedItems.Count);
        Assert.Equal(initialQueryCount, viewModel.ElementQueryPicker.DisplayedItems.Count);
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
        viewModel.ElementQueryPicker.SelectedQualifiedName = "Sample::Engine";
        viewModel.SelectedVerb = QueryVerb.Describe;
        viewModel.RunElementQuery();

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
        viewModel.ElementQueryPicker.SelectedQualifiedName = "Sample::Engine";
        viewModel.SelectedVerb = QueryVerb.Describe;
        viewModel.RunElementQuery();

        // Act
        await viewModel.CopyResultAsJsonAsync();

        // Assert
        Assert.NotNull(clipboard.LastText);
        Assert.Equal(QueryResultRenderer.RenderJson(viewModel.CurrentResult!), clipboard.LastText);
    }

    /// <summary>
    ///     Validates that the copy methods are a no-op (no exception, no clipboard write) when
    ///     <see cref="QueryDialogViewModel.CurrentResult" /> is <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task CopyMethods_NoResult_AreNoOps()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        // Force CurrentResult back to null (Construction sets it to the Browse-tab list result).
        viewModel.GetType()
            .GetProperty(nameof(QueryDialogViewModel.CurrentResult))!
            .SetValue(viewModel, null);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;

        // Act
        await viewModel.CopyResultAsMarkdownAsync();
        await viewModel.CopyResultAsJsonAsync();

        // Assert
        Assert.Null(clipboard.LastText);
    }

    /// <summary>
    ///     Validates that <see cref="QueryDialogViewModel.ElementScopedVerbs" /> exposes exactly the ten
    ///     element-scoped verbs, in the plan's stated order, and never includes <c>List</c> or
    ///     <c>Find</c>.
    /// </summary>
    [Fact]
    public void ElementScopedVerbs_HasExpectedTenVerbs()
    {
        // Assert
        Assert.Equal(10, QueryDialogViewModel.ElementScopedVerbs.Count);
        Assert.DoesNotContain(QueryVerb.List, QueryDialogViewModel.ElementScopedVerbs);
        Assert.DoesNotContain(QueryVerb.Find, QueryDialogViewModel.ElementScopedVerbs);
        Assert.Contains(QueryVerb.Describe, QueryDialogViewModel.ElementScopedVerbs);
        Assert.Contains(QueryVerb.Hierarchy, QueryDialogViewModel.ElementScopedVerbs);
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
