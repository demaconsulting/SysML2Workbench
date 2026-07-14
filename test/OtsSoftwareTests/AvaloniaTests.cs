using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS Avalonia requirements in docs/reqstream/ots/avalonia.yaml: that the workbench's real
///     <see cref="MainWindowView" /> window hosts its shell controls and diagram/diagnostics regions using
///     Avalonia's headless test platform (<see cref="AvaloniaFactAttribute" />), rather than a mocked or
///     hand-rolled UI harness.
/// </summary>
public sealed class AvaloniaTests : IDisposable
{
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-ots-avalonia-").FullName;
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-ots-avalonia-logs-").FullName;

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
    ///     Validates that Avalonia hosts the desktop shell's window and Dock-composed control tree: the menu,
    ///     the predefined-views list, and the custom-view builder panel are all real Avalonia controls attached
    ///     to the window's visual tree, even though each panel now lives in its own separately-named-scoped
    ///     UserControl hosted by the Dock layout rather than directly in the window.
    /// </summary>
    [AvaloniaFact]
    public void Startup_HostsDesktopShellControls()
    {
        // Arrange / Act
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Assert: the menu, predefined-view list, and custom-view builder controls are all real, discoverable
        // Avalonia controls hosted somewhere in the window's visual tree, regardless of which Dock-hosted
        // UserControl's own NameScope they belong to
        Assert.NotNull(window.GetVisualDescendants().OfType<Menu>().FirstOrDefault());
        Assert.NotNull(FindByName<ListBox>(window, "PredefinedViewsListBox"));
        Assert.NotNull(FindByName<ComboBox>(window, "ViewKindComboBox"));
        Assert.NotNull(FindByName<ListBox>(window, "AvailableExposeTargetsListBox"));
        Assert.NotNull(FindByName<Button>(window, "AddExposeTargetButton"));
        Assert.NotNull(FindByName<Button>(window, "PreviewCustomViewButton"));
        Assert.NotNull(FindByName<Button>(window, "CopyAsSysmlButton"));

        window.Close();
    }

    /// <summary>
    ///     Validates that Avalonia hosts the interactive diagram surface alongside the diagnostics panel: no
    ///     diagram tab (and so no <c>DiagramImage</c> control) exists until a view is selected, and opening a
    ///     workspace and selecting a view actually populates a real diagram control in the Avalonia visual tree
    ///     with the rendered content.
    /// </summary>
    [AvaloniaFact]
    public async Task MainWindow_HostsDiagramAndDiagnosticsPanels()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    view PredefinedView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n");
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var diagnosticsList = FindByName<ListBox>(window, "DiagnosticsListBox");

        // Assert: the diagnostics tool panel is hosted, and no diagram tab (so no DiagramImage control) exists
        // before any workspace is opened - the diagram document area is dynamically populated per open tab.
        Assert.NotNull(diagnosticsList);
        Assert.Null(FindByName<Image>(window, "DiagramImage"));

        // Act: opening the workspace and selecting the predefined view through the shell
        await shell.OpenWorkspaceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        Dispatcher.UIThread.RunJobs();

        // Assert: the shell's canvas host reflects the rendered diagram, and a real diagram Image control is now
        // hosted in the visual tree, bound to that same tab's canvas state through DiagramDocumentView's
        // code-behind
        Assert.True(shell.Canvas.IsContentLoaded);
        var diagramImage = FindByName<Image>(window, "DiagramImage");
        Assert.NotNull(diagramImage);
        Assert.NotNull(diagramImage!.Source);

        window.Close();
    }

    /// <summary>
    ///     Finds a named control anywhere in <paramref name="root" />'s visual tree, regardless of which
    ///     Dock-hosted UserControl's own compiled NameScope it belongs to (unlike a control's own
    ///     <c>FindControl{T}</c>, which only resolves names within its own NameScope).
    /// </summary>
    private static T? FindByName<T>(Visual root, string name) where T : Control
    {
        return root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name);
    }
}
