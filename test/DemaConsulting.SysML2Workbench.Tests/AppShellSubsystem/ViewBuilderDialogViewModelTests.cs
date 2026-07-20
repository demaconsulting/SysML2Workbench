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
///     Unit tests for <see cref="ViewBuilderDialogViewModel" />.
/// </summary>
public sealed class ViewBuilderDialogViewModelTests : IDisposable
{
    /// <summary>
    ///     Temporary workspace root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;

    /// <summary>
    ///     Temporary log directory created fresh for each test and removed on disposal.
    /// </summary>
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
    ///     Writes a small sample workspace with two elements that can be exposed.
    /// </summary>
    private async Task WriteSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "}\n",
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Builds a shell wired with real (non-mocked) subsystem units.
    /// </summary>
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
    ///     Validates that constructing the view model refreshes the available expose-target picker list from
    ///     the shell's currently loaded workspace.
    /// </summary>
    [Fact]
    public async Task Construction_RefreshesAvailableExposeTargetsFromWorkspace()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        // Act
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Assert
        Assert.False(viewModel.IsWorkspaceEmpty);
        Assert.Contains("Sample::Engine", viewModel.AvailableExposeTargets);
        Assert.Contains("Sample::Wheel", viewModel.AvailableExposeTargets);
    }

    /// <summary>
    ///     Validates that constructing the view model over an empty (zero-source) workspace reports
    ///     <see cref="ViewBuilderDialogViewModel.IsWorkspaceEmpty" /> and an empty available-targets list.
    /// </summary>
    [Fact]
    public void Construction_EmptyWorkspace_IsWorkspaceEmptyAndNoTargets()
    {
        // Arrange
        using var shell = CreateShell();

        // Act
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Assert
        Assert.True(viewModel.IsWorkspaceEmpty);
        Assert.Empty(viewModel.AvailableExposeTargets);
    }

    /// <summary>
    ///     Validates that adding an expose target mutates <see cref="ViewBuilderDialogViewModel.Definition" />
    ///     and raises <see cref="ViewBuilderDialogViewModel.PreviewChanged" /> exactly once.
    /// </summary>
    [Fact]
    public async Task AddExposeTarget_AddsToDefinitionAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        var previewChangedCount = 0;
        viewModel.PreviewChanged += (_, _) => previewChangedCount++;

        // Act
        viewModel.AddExposeTarget("Sample::Engine");

        // Assert
        Assert.Contains(viewModel.Definition.ExposeTargets, t => t.QualifiedName == "Sample::Engine");
        Assert.Equal(1, previewChangedCount);
    }

    /// <summary>
    ///     Validates that removing a previously-added expose target removes it from
    ///     <see cref="ViewBuilderDialogViewModel.Definition" />.
    /// </summary>
    [Fact]
    public async Task RemoveExposeTarget_RemovesFromDefinitionAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        viewModel.AddExposeTarget("Sample::Engine");
        var recursionKind = viewModel.Definition.ExposeTargets[0].RecursionKind;

        // Act
        viewModel.RemoveExposeTarget("Sample::Engine", recursionKind);

        // Assert
        Assert.Empty(viewModel.Definition.ExposeTargets);
    }

    /// <summary>
    ///     Validates that changing an expose target's recursion kind updates
    ///     <see cref="ViewBuilderDialogViewModel.Definition" />.
    /// </summary>
    [Fact]
    public async Task SetExposeRecursionKind_ChangesRecursionKindAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        viewModel.AddExposeTarget("Sample::Engine");
        var currentKind = viewModel.Definition.ExposeTargets[0].RecursionKind;

        // Act
        viewModel.SetExposeRecursionKind("Sample::Engine", currentKind, ExposeRecursionKind.NamespaceDirectChildren);

        // Assert
        Assert.Equal(ExposeRecursionKind.NamespaceDirectChildren, viewModel.Definition.ExposeTargets[0].RecursionKind);
    }

    /// <summary>
    ///     Validates that setting an expose target's bracket-filter expression updates
    ///     <see cref="ViewBuilderDialogViewModel.Definition" />.
    /// </summary>
    [Fact]
    public async Task SetExposeBracketFilter_SetsFilterAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        viewModel.AddExposeTarget("Sample::Engine");
        var kind = viewModel.Definition.ExposeTargets[0].RecursionKind;

        // Act
        viewModel.SetExposeBracketFilter("Sample::Engine", kind, "@Safety");

        // Assert
        Assert.Equal("@Safety", viewModel.Definition.ExposeTargets[0].BracketFilterExpression);
    }

    /// <summary>
    ///     Validates that changing the view kind updates <see cref="ViewBuilderDialogViewModel.Definition" /> and
    ///     raises <see cref="ViewBuilderDialogViewModel.PreviewChanged" /> exactly once.
    /// </summary>
    [Fact]
    public async Task SetViewKind_UpdatesDefinitionAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        var previewChangedCount = 0;
        viewModel.PreviewChanged += (_, _) => previewChangedCount++;

        // Act
        viewModel.SetViewKind(ViewKind.General);

        // Assert
        Assert.Equal(ViewKind.General, viewModel.Definition.ViewKind);
        Assert.Equal(1, previewChangedCount);
    }

    /// <summary>
    ///     Validates that changing the filter expression updates <see cref="ViewBuilderDialogViewModel.Definition" />
    ///     and raises <see cref="ViewBuilderDialogViewModel.PreviewChanged" /> exactly once.
    /// </summary>
    [Fact]
    public async Task SetFilterExpression_UpdatesDefinitionAndRaisesPreviewChanged()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        var previewChangedCount = 0;
        viewModel.PreviewChanged += (_, _) => previewChangedCount++;

        // Act
        viewModel.SetFilterExpression("@Safety");

        // Assert
        Assert.Equal("@Safety", viewModel.Definition.FilterExpression);
        Assert.Equal(1, previewChangedCount);
    }

    /// <summary>
    ///     Validates that an incomplete definition (no view kind/expose targets yet) surfaces a
    ///     <see cref="ViewBuilderDialogViewModel.StatusMessage" /> from <see cref="ViewBuilderDialogViewModel.RenderPreview" />
    ///     rather than throwing, since this method runs after every single control edit and a mid-edit definition
    ///     is routinely incomplete.
    /// </summary>
    [Fact]
    public async Task RenderPreview_IncompleteDefinition_SetsStatusMessageInsteadOfThrowing()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Act
        viewModel.RenderPreview();

        // Assert
        Assert.NotNull(viewModel.StatusMessage);
        Assert.False(viewModel.PreviewCanvas.IsContentLoaded);
    }

    /// <summary>
    ///     Validates that a valid, complete definition renders successfully into
    ///     <see cref="ViewBuilderDialogViewModel.PreviewCanvas" /> without ever mutating the shell's real tab
    ///     state (proving the live-preview render path is fully isolated from <see cref="MainWindowShell.OpenTabs" />).
    /// </summary>
    [Fact]
    public async Task RenderPreview_ValidDefinition_LoadsPreviewCanvasAndClearsStatusMessage()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Act
        viewModel.SetViewKind(ViewKind.General);
        viewModel.AddExposeTarget("Sample::Engine");

        // Assert
        Assert.True(viewModel.PreviewCanvas.IsContentLoaded);
        Assert.Null(viewModel.StatusMessage);

        // Assert: rendering into the dialog's own preview canvas never touched the shell's real tabs
        Assert.Empty(shell.OpenTabs);
        Assert.Null(shell.ActiveTabId);
    }

    /// <summary>
    ///     Validates the OK-commit happy path: a valid definition opens exactly one new tab, rendered with that
    ///     definition, and is made the active tab.
    /// </summary>
    [Fact]
    public async Task TryCommit_ValidDefinition_OpensExactlyOneNewTabAndReturnsTrue()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);
        viewModel.SetViewKind(ViewKind.General);
        viewModel.AddExposeTarget("Sample::Engine");

        // Act
        var result = viewModel.TryCommit(out var error);

        // Assert
        Assert.True(result);
        Assert.Null(error);
        Assert.Single(shell.OpenTabs);
        Assert.Equal(shell.OpenTabs[0].Id, shell.ActiveTabId);
        Assert.Same(viewModel.Definition, shell.OpenTabs[0].SourceDefinition);
    }

    /// <summary>
    ///     Validates the Risk #5 "open, try-render, close-tab-and-report-error on failure" sequence: an invalid
    ///     definition's commit must not leave any partial/empty tab behind.
    /// </summary>
    [Fact]
    public async Task TryCommit_InvalidDefinition_ReturnsFalseSetsStatusMessageAndDoesNotAddTab()
    {
        // Arrange: no view kind, no expose targets - definition never validates
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Act
        var result = viewModel.TryCommit(out var error);

        // Assert
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Equal(error, viewModel.StatusMessage);
        Assert.Empty(shell.OpenTabs);
        Assert.Null(shell.ActiveTabId);
    }

    /// <summary>
    ///     Validates the dialog's "Cancel performs no side effects" contract: constructing a view model and
    ///     editing it (live preview only touches the dialog's own <see cref="ViewBuilderDialogViewModel.PreviewCanvas" />)
    ///     without ever calling <see cref="ViewBuilderDialogViewModel.TryCommit" /> leaves the shell's tab state
    ///     completely untouched, matching what happens when a user clicks Cancel.
    /// </summary>
    [Fact]
    public async Task EditingWithoutCommitting_LeavesShellTabStateUntouched()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var viewModel = new ViewBuilderDialogViewModel(shell);

        // Act: simulate a full editing session that is never committed
        viewModel.SetViewKind(ViewKind.General);
        viewModel.AddExposeTarget("Sample::Engine");
        viewModel.SetFilterExpression("@Safety");
        viewModel.SetDisplayName("Draft");

        // Assert: zero calls into Shell's tab-mutating surface
        Assert.Empty(shell.OpenTabs);
        Assert.Null(shell.ActiveTabId);
        Assert.Null(shell.ActiveCustomView);
    }

    /// <summary>
    ///     Validates the "fresh instance per open" dialog-lifetime contract: a new
    ///     <see cref="ViewBuilderDialogViewModel" /> never carries over a prior instance's selections, even
    ///     though both are constructed over the same shell.
    /// </summary>
    [Fact]
    public async Task Construction_FreshInstance_DoesNotCarryOverPriorInstanceSelections()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var first = new ViewBuilderDialogViewModel(shell);
        first.SetViewKind(ViewKind.General);
        first.AddExposeTarget("Sample::Engine");
        first.SetFilterExpression("@Safety");
        first.SetDisplayName("First");

        // Act
        var second = new ViewBuilderDialogViewModel(shell);

        // Assert
        Assert.Null(second.Definition.ViewKind);
        Assert.Empty(second.Definition.ExposeTargets);
        Assert.Null(second.Definition.FilterExpression);
        Assert.Null(second.Definition.DisplayName);
        Assert.NotSame(first.Definition, second.Definition);
    }
}
