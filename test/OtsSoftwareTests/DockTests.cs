using Avalonia.Headless.XUnit;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS Dock requirements in docs/reqstream/ots/dock.yaml: that
///     <see cref="WorkbenchDockFactory" /> genuinely composes a Dock layout from the four Phase-0 panel view
///     models, and that Avalonia's real <see cref="DockControl" /> can host that layout, using Avalonia's
///     headless test platform (<see cref="AvaloniaFactAttribute" />) rather than a mocked or hand-rolled harness.
/// </summary>
public sealed class DockTests : IDisposable
{
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-ots-dock-logs-").FullName;

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
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new SvgCanvasHost(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Validates that <see cref="WorkbenchDockFactory.CreateLayout" /> returns a non-null root dock whose
    ///     visual tree of dockables includes the four Phase-0 panel view models, proving the factory genuinely
    ///     composes the documented layout rather than an empty or partial one.
    /// </summary>
    [Fact]
    public void CreateLayout_ComposesFourPanelDockables()
    {
        // Arrange
        using var shell = CreateShell();
        var diagramViewModel = new DiagramDocumentViewModel(shell);
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell, diagramViewModel);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell, diagramViewModel);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel, diagramViewModel);

        // Act
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        // Assert
        Assert.NotNull(layout);
        var dockables = CollectDockables(layout).ToList();
        Assert.Contains(predefinedViewsViewModel, dockables);
        Assert.Contains(customViewBuilderViewModel, dockables);
        Assert.Contains(diagnosticsViewModel, dockables);
        Assert.Contains(diagramViewModel, dockables);
    }

    /// <summary>
    ///     Validates that a real Avalonia <see cref="DockControl" /> hosts a <see cref="WorkbenchDockFactory" />
    ///     layout without throwing, under the headless test platform.
    /// </summary>
    [AvaloniaFact]
    public void DockControl_HostsWorkbenchLayout()
    {
        // Arrange
        using var shell = CreateShell();
        var diagramViewModel = new DiagramDocumentViewModel(shell);
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell, diagramViewModel);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell, diagramViewModel);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel, diagramViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        // Act
        var window = new Avalonia.Controls.Window
        {
            Content = new DockControl { Layout = layout },
        };
        window.Show();

        // Assert
        Assert.NotNull(window.Content);

        window.Close();
    }

    /// <summary>
    ///     Walks a dock tree's visible dockables, recursing into nested docks, so the test can confirm every leaf
    ///     panel view model is genuinely reachable from the root.
    /// </summary>
    private static IEnumerable<IDockable> CollectDockables(IDockable dockable)
    {
        yield return dockable;

        if (dockable is IDock dock && dock.VisibleDockables is not null)
        {
            foreach (var child in dock.VisibleDockables)
            {
                foreach (var descendant in CollectDockables(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
