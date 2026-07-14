using System.Linq;
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
using Dock.Model.Core.Events;

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
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Validates that <see cref="WorkbenchDockFactory.CreateLayout" /> returns a non-null root dock whose
    ///     visual tree of dockables includes the three Phase-0 Tool panel view models and a dynamically added
    ///     diagram document, proving the factory genuinely composes the documented layout rather than an empty or
    ///     partial one.
    /// </summary>
    [Fact]
    public void CreateLayout_ComposesFourPanelDockables()
    {
        // Arrange
        using var shell = CreateShell();
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);

        // Act
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        var diagramViewModel = new DiagramDocumentViewModel(shell, "tab-1");
        factory.AddDockable(factory.DiagramDock, diagramViewModel);

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
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        factory.AddDockable(factory.DiagramDock, new DiagramDocumentViewModel(shell, "tab-1"));

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
    ///     Validates that closing a Tool panel through <see cref="WorkbenchDockFactory" />'s
    ///     <c>HideToolsOnClose = true</c> setting hides it (rather than destroying it) and that
    ///     <c>RestoreDockable</c> then brings the exact same view model instance back to its original
    ///     <see cref="IToolDock" />, proving the "View" menu's close/restore round-trip is genuinely backed by
    ///     Dock's own hide/restore API rather than reconstructing a new panel instance.
    /// </summary>
    [AvaloniaFact]
    public void RestoreDockable_ReopensClosedToolInOriginalDock()
    {
        // Arrange
        using var shell = CreateShell();
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        var window = new Avalonia.Controls.Window
        {
            Content = new DockControl { Layout = layout },
        };
        window.Show();

        var originalOwner = Assert.IsAssignableFrom<IToolDock>(predefinedViewsViewModel.Owner);
        Assert.Contains(predefinedViewsViewModel, originalOwner.VisibleDockables!);

        // Act - close the panel through the same public API Dock's own chrome invokes.
        factory.CloseDockable(predefinedViewsViewModel);

        // Assert - the panel is hidden, not destroyed: removed from its owner's VisibleDockables and tracked
        // in the root dock's HiddenDockables, with the same view model instance still intact.
        Assert.DoesNotContain(predefinedViewsViewModel, originalOwner.VisibleDockables ?? Enumerable.Empty<IDockable>());
        Assert.Contains(predefinedViewsViewModel, layout.HiddenDockables!);

        // Act - restore the panel via the same API the View menu's Click handler calls.
        factory.RestoreDockable(predefinedViewsViewModel);
        factory.SetActiveDockable(predefinedViewsViewModel);
        if (predefinedViewsViewModel.Owner is IDock restoredOwnerDock)
        {
            factory.SetFocusedDockable(restoredOwnerDock, predefinedViewsViewModel);
        }

        // Assert - the exact same instance is back in its original dock, and no longer tracked as hidden.
        Assert.DoesNotContain(predefinedViewsViewModel, layout.HiddenDockables ?? Enumerable.Empty<IDockable>());
        Assert.Contains(predefinedViewsViewModel, originalOwner.VisibleDockables!);
        Assert.Same(originalOwner, predefinedViewsViewModel.Owner);

        window.Close();
    }

    /// <summary>
    ///     Validates the empty-<c>DocumentDock</c>-persistence research finding end to end through a real
    ///     Dock control: building the layout with zero initial diagram documents, adding one, then closing it
    ///     leaves the same <see cref="WorkbenchDockFactory.DiagramDock" /> instance in the layout tree with zero
    ///     visible dockables, rather than the container itself disappearing.
    /// </summary>
    [AvaloniaFact]
    public void DocumentDock_StaysInLayoutTree_AfterLastDocumentCloses()
    {
        // Arrange
        using var shell = CreateShell();
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        var diagramDocument = new DiagramDocumentViewModel(shell, "tab-1");
        factory.AddDockable(factory.DiagramDock, diagramDocument);

        var window = new Avalonia.Controls.Window
        {
            Content = new DockControl { Layout = layout },
        };
        window.Show();

        // Act
        factory.CloseDockable(diagramDocument);

        // Assert
        Assert.Contains(factory.DiagramDock, CollectDockables(layout));
        Assert.Empty(factory.DiagramDock.VisibleDockables ?? Enumerable.Empty<IDockable>());

        window.Close();
    }

    /// <summary>
    ///     Validates dynamic add/remove of diagram documents at runtime, and that closing one raises
    ///     <see cref="WorkbenchDockFactory.DiagramTabClosed" /> exactly once with the closed view model.
    /// </summary>
    [AvaloniaFact]
    public void AddDockable_And_CloseDockable_DynamicallyManageDiagramDocuments()
    {
        // Arrange
        using var shell = CreateShell();
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        var window = new Avalonia.Controls.Window
        {
            Content = new DockControl { Layout = layout },
        };
        window.Show();

        var firstDocument = new DiagramDocumentViewModel(shell, "tab-1");
        var secondDocument = new DiagramDocumentViewModel(shell, "tab-2");
        factory.AddDockable(factory.DiagramDock, firstDocument);
        factory.AddDockable(factory.DiagramDock, secondDocument);

        // Assert - both added
        Assert.Contains(firstDocument, factory.DiagramDock.VisibleDockables!);
        Assert.Contains(secondDocument, factory.DiagramDock.VisibleDockables!);
        Assert.Equal(2, factory.DiagramDock.VisibleDockables!.Count);

        var closedDocuments = new List<DiagramDocumentViewModel>();
        factory.DiagramTabClosed += (_, closed) => closedDocuments.Add(closed);

        // Act - close one
        factory.CloseDockable(firstDocument);

        // Assert
        Assert.Single(factory.DiagramDock.VisibleDockables!);
        Assert.Contains(secondDocument, factory.DiagramDock.VisibleDockables!);
        Assert.DoesNotContain(firstDocument, factory.DiagramDock.VisibleDockables!);
        Assert.Equal([firstDocument], closedDocuments);

        window.Close();
    }

    /// <summary>
    ///     Validates that <see cref="Dock.Model.Core.IFactory.FocusedDockableChanged" /> fires for a programmatic
    ///     active-dockable change onto a diagram document, the mechanism <see cref="MainWindowView" /> relies on
    ///     to forward Dock's own focus tracking to <see cref="MainWindowShell.NotifyActiveDiagramTab" />.
    /// </summary>
    [Fact]
    public void FocusedDockableChanged_FiresForActiveDockableChangesOnDiagramDocuments()
    {
        // Arrange
        using var shell = CreateShell();
        var predefinedViewsViewModel = new PredefinedViewsToolViewModel(shell);
        var customViewBuilderViewModel = new CustomViewBuilderToolViewModel(shell);
        var diagnosticsViewModel = new DiagnosticsToolViewModel(shell);
        var factory = new WorkbenchDockFactory(predefinedViewsViewModel, customViewBuilderViewModel, diagnosticsViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        var firstDocument = new DiagramDocumentViewModel(shell, "tab-1");
        var secondDocument = new DiagramDocumentViewModel(shell, "tab-2");
        factory.AddDockable(factory.DiagramDock, firstDocument);
        factory.AddDockable(factory.DiagramDock, secondDocument);

        FocusedDockableChangedEventArgs? received = null;
        factory.FocusedDockableChanged += (_, e) => received = e;

        // Act
        factory.SetFocusedDockable(factory.DiagramDock, secondDocument);

        // Assert
        Assert.NotNull(received);
        Assert.Same(secondDocument, received.Dockable);
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
