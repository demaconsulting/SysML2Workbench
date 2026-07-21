using System.Reflection;
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
    ///     Regression test for the "Copy as SysML always returns the last-created custom view" bug: opening the
    ///     View Builder's real "OK" commit sequence twice (<see cref="MainWindowShell.OpenNewCustomPreviewTab" />
    ///     immediately followed by <see cref="MainWindowShell.PreviewCustomView" />, exactly as
    ///     <c>ViewBuilderDialogViewModel.TryCommit</c> does) creates two distinct, real
    ///     <see cref="DiagramDocumentView" /> tabs hosted by the real Dock-composed <see cref="MainWindowView" />.
    ///     Selecting the first tab and copying must yield the first view's snippet, not the second (previously
    ///     failing because <c>DiagramDocumentView.OnDataContextChanged</c> bound the clipboard service only once
    ///     per view model via <c>??=</c>, so re-selecting a tab whose view had been detached and later
    ///     re-attached left its clipboard write silently targeting a stale, detached anchor).
    /// </summary>
    [AvaloniaFact]
    public async Task DiagramContextMenu_CopyAsSysml_TwoCustomViewTabs_EachCopiesItsOwnSnippet()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Gearbox;\n"
            + "}\n");
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        await shell.AddFolderSourceAsync(_tempRoot);
        Dispatcher.UIThread.RunJobs();

        // Act: commit the first custom view (General diagram over Engine), mirroring ViewBuilderDialog's real
        // OK-button sequence.
        var firstDefinition = new ViewDefinitionModel();
        firstDefinition.SetViewKind(ViewKind.General);
        firstDefinition.AddExposeTarget("Sample::Engine");
        shell.OpenNewCustomPreviewTab();
        shell.PreviewCustomView(firstDefinition);
        Dispatcher.UIThread.RunJobs();
        var firstTabId = shell.ActiveTabId!;
        var expectedFirstSnippet = shell.ExportTabAsSysmlSnippet(firstTabId);
        Assert.NotNull(expectedFirstSnippet);

        // Act: commit a second, distinct custom view (Interconnection diagram over Gearbox), which opens a
        // second real tab/DiagramDocumentView rather than re-rendering the first in place.
        var secondDefinition = new ViewDefinitionModel();
        secondDefinition.SetViewKind(ViewKind.Interconnection);
        secondDefinition.AddExposeTarget("Sample::Gearbox");
        shell.OpenNewCustomPreviewTab();
        shell.PreviewCustomView(secondDefinition);
        Dispatcher.UIThread.RunJobs();
        var secondTabId = shell.ActiveTabId!;
        var expectedSecondSnippet = shell.ExportTabAsSysmlSnippet(secondTabId);
        Assert.NotNull(expectedSecondSnippet);
        Assert.NotEqual(firstTabId, secondTabId);
        Assert.NotEqual(expectedFirstSnippet, expectedSecondSnippet);

        // Act: switch back to the first tab and copy - this is the step that previously returned the second
        // (last-created) tab's snippet regardless of which tab was actually active. Uses the real Dock factory's
        // SetActiveDockable/SetFocusedDockable, exactly as Dock's own tab-header click handler does, so this
        // faithfully reproduces a user clicking back to the first tab rather than merely poking shell state.
        SelectDiagramTab(window, firstTabId);
        Dispatcher.UIThread.RunJobs();
        var firstCopiedSnippet = await CopyActiveTabAsSysmlAsync(window);

        // Assert: the first tab's own snippet was copied, not the second tab's.
        Assert.Equal(expectedFirstSnippet, firstCopiedSnippet);

        // Act: switch to the second tab and copy too, confirming it still copies its own snippet correctly.
        SelectDiagramTab(window, secondTabId);
        Dispatcher.UIThread.RunJobs();
        var secondCopiedSnippet = await CopyActiveTabAsSysmlAsync(window);

        // Assert
        Assert.Equal(expectedSecondSnippet, secondCopiedSnippet);

        window.Close();
    }

    /// <summary>
    ///     Selects the diagram document tab with the given tab id via the real Dock factory's
    ///     <c>SetActiveDockable</c>/<c>SetFocusedDockable</c> - exactly what Dock's own tab-header click handler
    ///     invokes - so the resulting <see cref="MainWindowShell.NotifyActiveDiagramTab" /> notification (raised
    ///     through <see cref="MainWindowView" />'s <c>FocusedDockableChanged</c> forwarding) faithfully reflects a
    ///     real user tab click rather than only updating shell-side bookkeeping. Reached via reflection since
    ///     <see cref="MainWindowView" />'s Dock factory and per-tab view-model tracking are private implementation
    ///     details not otherwise exposed to tests.
    /// </summary>
    private static void SelectDiagramTab(MainWindowView window, string tabId)
    {
        var windowType = typeof(MainWindowView);
        var dockFactoryField = windowType.GetField("_dockFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        var tabViewModelsField = windowType.GetField("_tabViewModelsByTabId", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(dockFactoryField);
        Assert.NotNull(tabViewModelsField);

        var dockFactory = (WorkbenchDockFactory)dockFactoryField!.GetValue(window)!;
        var tabViewModels = (Dictionary<string, Dock.Model.Mvvm.Controls.Document>)tabViewModelsField!.GetValue(window)!;
        var tabViewModel = tabViewModels[tabId];

        dockFactory.SetActiveDockable(tabViewModel);
        dockFactory.SetFocusedDockable(dockFactory.DiagramDock, tabViewModel);
    }

    /// <summary>
    ///     Drives the real "Copy as SysML" context-menu <c>MenuItem</c> for whichever <see cref="DiagramDocumentView" />
    ///     is currently hosted by <paramref name="window" />, and returns whatever text ends up on the headless
    ///     platform's real clipboard afterward.
    /// </summary>
    private static async Task<string?> CopyActiveTabAsSysmlAsync(MainWindowView window)
    {
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

        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        Assert.NotNull(clipboard);
        return await clipboard!.TryGetTextAsync();
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

    /// <summary>
    ///     Walks the logical tree rooted at <paramref name="root" /> and enumerates every
    ///     <see cref="MenuItem" /> it contains, including submenu items whose visual is only realized
    ///     when the parent menu opens.
    /// </summary>
    private static IEnumerable<MenuItem> LogicalTreeMenuItems(Avalonia.LogicalTree.ILogical root)
    {
        foreach (var child in root.LogicalChildren)
        {
            if (child is MenuItem menuItem)
            {
                yield return menuItem;
            }

            foreach (var descendant in LogicalTreeMenuItems(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    ///     End-to-end regression for the redesigned Query dialog: constructs a real
    ///     <see cref="MainWindowView" />, confirms its Query menu hosts the "Run Query..." entry (the
    ///     view-side counterpart of the plan's new <c>_Query</c> top-level menu), then opens the
    ///     modal <see cref="QueryDialogView" /> directly (rather than through the modal
    ///     <c>ShowDialog</c> path, which blocks its calling turn until closed), confirms the dialog
    ///     opens on the "List" Query Type with the selection-free <c>ListFilterView</c> visible and the
    ///     selectable <c>ElementQueryPickerView</c> hidden, selects the "Describe" entry on the Query
    ///     Type combo (confirming the two controls' visibility flips), selects an element on the
    ///     now-visible picker, and confirms the real
    ///     <see cref="DemaConsulting.SysML2Tools.Query.QueryEngine" /> result appears immediately with
    ///     no "Run" gesture of any kind. Then right-clicks the results panel's context menu's "Copy as
    ///     Markdown" entry and asserts the headless platform's real clipboard now holds the exact text
    ///     <see cref="DemaConsulting.SysML2Tools.Query.QueryResultRenderer.RenderMarkdown" /> produces.
    ///     Mirrors <see cref="DiagramContextMenu_CopyAsSysml_CopiesSnippetToClipboard" />'s right-click
    ///     recipe.
    /// </summary>
    [AvaloniaFact]
    public async Task QueryDialog_SelectDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard()
    {
        // Arrange: a real workspace with one part def, so Describe has something meaningful to say
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "}\n");
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        await shell.AddFolderSourceAsync(_tempRoot);
        Dispatcher.UIThread.RunJobs();

        // Assert the Query menu item is really wired in the main window's logical tree. It lives
        // under the top-level "_Query" menu which Avalonia lazily materializes in the visual tree
        // only after the menu opens, so we walk the logical tree (which mirrors the AXAML structure
        // regardless of visualization state) instead of using FindByName's visual-tree walk.
        var queryMenuItem = LogicalTreeMenuItems(window)
            .FirstOrDefault(mi => mi.Name == "QueryDialogMenuItem");
        Assert.NotNull(queryMenuItem);

        // Act: open the dialog directly with the same shell reference OnOpenQueryDialogClick would use.
        var dialog = new QueryDialogView(shell);
        dialog.Show(window);
        Dispatcher.UIThread.RunJobs();

        // Assert: the dialog opens defaulting to "List" Query Type, so ListFilterView (the
        // selection-free filter control) is visible and ElementQueryPickerView (whose selection would
        // otherwise be silently ignored for "List") is hidden.
        var listFilterView = FindByName<UserControl>(dialog, "ListFilterView");
        var elementQueryPickerView = FindByName<UserControl>(dialog, "ElementQueryPickerView");
        Assert.NotNull(listFilterView);
        Assert.NotNull(elementQueryPickerView);
        Assert.True(listFilterView!.IsVisible);
        Assert.False(elementQueryPickerView!.IsVisible);

        // Act: select "Describe" on the Query Type combo. Describe is no longer the VM's
        // construction-time default now that the default Query Type is "List", so this step itself
        // exercises the Query Type combo's wiring.
        var queryTypeComboBox = FindByName<ComboBox>(dialog, "QueryTypeComboBox");
        Assert.NotNull(queryTypeComboBox);
        queryTypeComboBox!.SelectedItem = DemaConsulting.SysML2Tools.Query.QueryVerb.Describe;
        Dispatcher.UIThread.RunJobs();

        var vm = (QueryDialogViewModel)dialog.DataContext!;
        Assert.Equal(DemaConsulting.SysML2Tools.Query.QueryVerb.Describe, vm.SelectedQueryType);

        // Assert: switching to an element-scoped Query Type flips the two controls' visibility, and now
        // that ElementQueryPickerView is visible its inner candidate ListBox is realized - no
        // TabControl/tab-switch exists in this design.
        Assert.False(listFilterView.IsVisible);
        Assert.True(elementQueryPickerView.IsVisible);
        var pickerListBox = FindByName<ListBox>(dialog, "PickerItemsListBox");
        Assert.NotNull(pickerListBox);

        // Act: select the target element on the single picker - this alone must produce the result,
        // with no "Run" button or method of any kind in this design.
        vm.Picker.SelectedQualifiedName = "Sample::Engine";
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(vm.CurrentResult);
        Assert.Equal("describe", vm.CurrentResult!.Verb);
        Assert.Equal("Sample::Engine", vm.CurrentResult.Element);

        // Act: right-click the results panel (open its context menu) and click "Copy as Markdown",
        // mirroring DiagramContextMenu_CopyAsSysml_CopiesSnippetToClipboard's exact recipe.
        var resultsBorder = FindByName<Border>(dialog, "ResultsBorder");
        Assert.NotNull(resultsBorder);
        var contextMenu = resultsBorder!.ContextMenu;
        Assert.NotNull(contextMenu);
        contextMenu!.Open(resultsBorder);
        Dispatcher.UIThread.RunJobs();

        var copyMarkdownMenuItem = FindByName<MenuItem>(dialog, "CopyAsMarkdownMenuItem");
        Assert.NotNull(copyMarkdownMenuItem);
        Assert.True(copyMarkdownMenuItem!.IsEnabled);
        copyMarkdownMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // Wait a beat for the async clipboard write to complete on the UI thread
        await Task.Yield();
        Dispatcher.UIThread.RunJobs();

        // Assert: the headless platform's real clipboard now holds the expected rendered Markdown
        var clipboard = TopLevel.GetTopLevel(dialog)?.Clipboard;
        Assert.NotNull(clipboard);
        var clipboardText = await clipboard!.TryGetTextAsync();
        var expected = string.Join(
            "\n",
            DemaConsulting.SysML2Tools.Query.QueryResultRenderer.RenderMarkdown(vm.CurrentResult));
        Assert.Equal(expected, clipboardText);

        dialog.Close();
        window.Close();
    }
}
