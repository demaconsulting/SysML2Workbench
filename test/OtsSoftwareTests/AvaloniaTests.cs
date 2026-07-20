using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
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
    ///     Validates that Avalonia hosts the desktop shell's window and Dock-composed control tree: the menu
    ///     and the predefined-views list are real Avalonia controls attached to the window's visual tree, even
    ///     though the panel lives in its own separately-named-scoped UserControl hosted by the Dock layout rather
    ///     than directly in the window.
    /// </summary>
    [AvaloniaFact]
    public void Startup_HostsDesktopShellControls()
    {
        // Arrange / Act
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Assert: the menu and predefined-view list are all real, discoverable Avalonia controls hosted
        // somewhere in the window's visual tree, regardless of which Dock-hosted UserControl's own NameScope
        // they belong to
        Assert.NotNull(window.GetVisualDescendants().OfType<Menu>().FirstOrDefault());
        Assert.NotNull(FindByName<ListBox>(window, "PredefinedViewsListBox"));

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
        await shell.AddFolderSourceAsync(_tempRoot);
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
    ///     Validates the real end-to-end "Copy as SysML" diagram-tab context-menu integration: opening a
    ///     workspace, rendering a predefined view into a real <see cref="DiagramDocumentView" /> hosted by
    ///     <see cref="MainWindowView" />, invoking its "Copy as SysML" <c>MenuItem</c>, and confirming the
    ///     headless platform's real <c>TopLevel.Clipboard</c> now holds the exact SysML snippet
    ///     <see cref="SysmlSnippetGenerator" /> produces for that view's definition.
    /// </summary>
    [AvaloniaFact]
    public async Task DiagramContextMenu_CopyAsSysml_CopiesSnippetToClipboard()
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

        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        Dispatcher.UIThread.RunJobs();

        var expectedSnippet = shell.ExportTabAsSysmlSnippet(shell.ActiveTabId!);
        Assert.NotNull(expectedSnippet);

        // Act: find the real diagram border, open its context menu, and click the "Copy as SysML" menu item
        var diagramBorder = FindByName<Border>(window, "DiagramBorder");
        Assert.NotNull(diagramBorder);
        var contextMenu = diagramBorder!.ContextMenu;
        Assert.NotNull(contextMenu);
        contextMenu!.Open(diagramBorder);
        Dispatcher.UIThread.RunJobs();

        var copyMenuItem = FindByName<MenuItem>(window, "CopyAsSysmlMenuItem");
        Assert.NotNull(copyMenuItem);
        Assert.True(copyMenuItem!.IsEnabled);
        copyMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // Assert: the headless platform's real clipboard now holds the expected SysML snippet
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        Assert.NotNull(clipboard);
        var clipboardText = await clipboard!.TryGetTextAsync();
        Assert.Equal(expectedSnippet, clipboardText);

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
