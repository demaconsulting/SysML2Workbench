using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.UiTests.AppShellSubsystem;

/// <summary>
///     Local view/view-model interaction tests for <see cref="QueryDialogView" />, covering the results
///     grid's optional-column behavior. That logic lives in the view's code-behind and addresses the
///     columns by index, so it cannot be reached from a view-model-only test.
/// </summary>
public sealed class QueryDialogUiTests : IDisposable
{
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-ui-tests-query-logs-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempLogRoot))
        {
            Directory.Delete(_tempLogRoot, recursive: true);
        }
    }

    private MainWindowShell CreateShell()
    {
        return new MainWindowShell(
            new MainWindowShellDependencies(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot)));
    }

    /// <summary>
    ///     Validates that the results grid declares its columns in the exact order the code-behind's
    ///     <c>ApplyResultVisibility</c> assumes when it toggles them by index.
    /// </summary>
    /// <remarks>
    ///     <c>DataGridColumn</c> does not participate in Avalonia's compiled-bindings field generation, so
    ///     the code-behind has no choice but to address columns positionally. That makes a reordering or
    ///     insertion in the AXAML silently toggle the wrong column - no exception, no binding error, just a
    ///     wrong panel. This test is the guard that turns such a change into a build-time failure instead.
    /// </remarks>
    [AvaloniaFact]
    public void QueryDialogView_EntriesDataGrid_DeclaresColumnsInIndexOrderCodeBehindAssumes()
    {
        // Arrange
        using var shell = CreateShell();
        var view = new QueryDialogView { DataContext = new QueryDialogViewModel(shell) };
        view.Show();
        Dispatcher.UIThread.RunJobs();

        // Act
        var grid = view.FindControl<DataGrid>("EntriesDataGrid");
        Assert.NotNull(grid);

        // Assert
        Assert.Equal(
            ["Qualified Name", "Kind", "Detail", "Direction", "Depth", "Relation", "Via"],
            grid.Columns.Select(column => column.Header as string));

        view.Close();
    }

    /// <summary>
    ///     Validates that the three traversal-metadata columns are shown only when at least one row in the
    ///     current result actually carries that value, so a verb that walks nothing renders exactly the
    ///     panel it did before those columns existed.
    /// </summary>
    [AvaloniaFact]
    public void QueryDialogView_TraversalColumns_AreShownOnlyWhenRowsCarryThoseValues()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new QueryDialogViewModel(shell);
        var view = new QueryDialogView { DataContext = viewModel };
        view.Show();
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("EntriesDataGrid");
        Assert.NotNull(grid);

        // Act: a result whose rows carry no traversal metadata at all
        viewModel.CurrentResultRows =
        [
            new QueryResultRow("A::b", "part", "detail", string.Empty, string.Empty, string.Empty, string.Empty, null),
        ];
        Dispatcher.UIThread.RunJobs();

        // Assert: none of the three optional columns intrude on a non-traversing result
        Assert.False(grid.Columns[4].IsVisible);
        Assert.False(grid.Columns[5].IsVisible);
        Assert.False(grid.Columns[6].IsVisible);

        // Act: a result carrying a depth and relation, but still no "via" roll-up
        viewModel.CurrentResultRows =
        [
            new QueryResultRow("A::b", "part", "detail", string.Empty, "1", "Connect", string.Empty, null),
        ];
        Dispatcher.UIThread.RunJobs();

        // Assert: each column tracks its own value independently rather than switching as a group
        Assert.True(grid.Columns[4].IsVisible);
        Assert.True(grid.Columns[5].IsVisible);
        Assert.False(grid.Columns[6].IsVisible);

        view.Close();
    }
}
