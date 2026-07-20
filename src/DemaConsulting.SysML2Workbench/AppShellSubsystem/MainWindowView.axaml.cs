using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Dock.Model.Core;
using Dock.Model.Core.Events;
using Dock.Model.Mvvm.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the main application window. All region-specific orchestration and
///     validation logic is delegated to <see cref="MainWindowShell" /> (via the panel view models this class
///     composes into a Dock layout); this class only builds that layout, wires the File, View, and Help menus, forwards
///     Dock's own focus/close signals to <see cref="MainWindowShell" />, and reconciles the shell's
///     <see cref="MainWindowShell.OpenTabs" /> against the Dock <see cref="WorkbenchDockFactory.DiagramDock" />.
///     The View menu lets a user restore a Tool panel that was closed through Dock's own chrome, reusing the same
///     long-lived panel view model instance so any in-progress panel state survives the close/restore cycle.
/// </summary>
public partial class MainWindowView : Window
{
    private static readonly FilePickerFileType SysmlFileType = new("SysML v2 files")
    {
        Patterns = ["*.sysml"],
    };

    private readonly MainWindowShell _shell;
    private readonly PredefinedViewsToolViewModel _predefinedViewsViewModel;
    private readonly DiagnosticsToolViewModel _diagnosticsViewModel;
    private readonly WorkspacePanelToolViewModel _workspacePanelViewModel;
    private readonly WorkbenchDockFactory _dockFactory;
    private readonly Dictionary<string, Dock.Model.Mvvm.Controls.Document> _tabViewModelsByTabId = new();

    /// <summary>
    ///     Parameterless constructor required by the Avalonia XAML previewer/designer. Not used at runtime.
    /// </summary>
    public MainWindowView()
        : this(DesignTimeShellFactory.Create())
    {
    }

    /// <summary>
    ///     Creates the main window bound to a real, composed shell, and builds the Dock layout hosting the
    ///     Phase-0 panels and every currently open diagram tab over it.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public MainWindowView(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));

        InitializeComponent();

        _predefinedViewsViewModel = new PredefinedViewsToolViewModel(_shell) { Id = "PredefinedViews", Title = "Predefined Views" };
        _diagnosticsViewModel = new DiagnosticsToolViewModel(_shell) { Id = "Diagnostics", Title = "Diagnostics" };
        _workspacePanelViewModel = new WorkspacePanelToolViewModel(_shell) { Id = "WorkspacePanel", Title = "Workspace" };

        var factory = new WorkbenchDockFactory(_predefinedViewsViewModel, _diagnosticsViewModel, _workspacePanelViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        WorkbenchDockControl.Layout = (IDock)layout;
        _dockFactory = factory;

        _dockFactory.FocusedDockableChanged += OnFocusedDockableChanged;
        _dockFactory.DiagramTabClosed += OnDiagramTabClosed;
        _dockFactory.SourceTextTabClosed += OnSourceTextTabClosed;
        _shell.TabsChanged += OnShellTabsChanged;
        OnShellTabsChanged(this, EventArgs.Empty);

        AddFileSourceMenuItem.Click += OnAddFileSourceClick;
        AddFolderSourceMenuItem.Click += OnAddFolderSourceClick;

        // Each View-menu item's DataContext is explicitly set to its panel view model (distinct from this
        // window's own DataContext) so the one-way IsChecked binding above resolves against the panel's
        // IsOpen property without requiring any change to the window's binding context.
        PredefinedViewsMenuItem.DataContext = _predefinedViewsViewModel;
        DiagnosticsMenuItem.DataContext = _diagnosticsViewModel;
        WorkspacePanelMenuItem.DataContext = _workspacePanelViewModel;

        AddHandler(DragDrop.DropEvent, OnWindowDrop);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
    }

    /// <summary>
    ///     Handles the View menu's "Predefined Views" click by showing or focusing that panel.
    /// </summary>
    private void OnPredefinedViewsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_predefinedViewsViewModel);
    }

    /// <summary>
    ///     Handles the View menu's "Workspace" click by showing or focusing that panel.
    /// </summary>
    private void OnWorkspacePanelMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_workspacePanelViewModel);
    }

    /// <summary>
    ///     Handles the View menu's "Diagnostics" click by showing or focusing that panel.
    /// </summary>
    private void OnDiagnosticsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_diagnosticsViewModel);
    }

    /// <summary>
    ///     Handles the File menu's "Exit" click by closing the main window, which shuts down the application
    ///     (this is the desktop app's only window).
    /// </summary>
    private void OnExitMenuItemClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    ///     Handles the Help menu's "About" click by showing the modal About dialog, owned by this window.
    /// </summary>
    private async void OnAboutMenuItemClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialogView();
        await dialog.ShowDialog(this);
    }

    /// <summary>
    ///     Handles the View menu's "Custom View Builder..." click by opening the modal
    ///     <see cref="ViewBuilderDialogView" />, owned by this window. Unlike the other View-menu items, this
    ///     opens a transient dialog (a fresh <see cref="ViewBuilderDialogViewModel" /> per open) rather than
    ///     showing/focusing a persistent Dock <c>Tool</c>.
    /// </summary>
    private async void OnOpenViewBuilderDialogClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ViewBuilderDialogView(_shell);
        await dialog.ShowDialog(this);
    }

    /// <summary>
    ///     Restores <paramref name="tool" /> to its original dock if it was hidden by a prior close (via
    ///     <see cref="WorkbenchDockFactory" />'s <c>HideToolsOnClose</c> setting, a safe no-op if it is not
    ///     currently hidden), then makes it the active and focused dockable in its owning dock. This never hides
    ///     an already-open panel: clicking a View-menu item for a visible panel simply (re)focuses it.
    /// </summary>
    /// <param name="tool">The panel to show or bring into focus, reusing its existing long-lived instance.</param>
    private void ShowOrFocusPanel(Tool tool)
    {
        _dockFactory.RestoreDockable(tool);
        _dockFactory.SetActiveDockable(tool);

        if (tool.Owner is IDock ownerDock)
        {
            _dockFactory.SetFocusedDockable(ownerDock, tool);
        }
    }

    /// <summary>
    ///     Forwards Dock's own focus-change signal to the shell, but only when focus lands on a diagram document
    ///     (focus changes onto a Tool panel are deliberately ignored, so switching focus to a tool panel does not
    ///     clear which diagram tab a subsequent "Preview" click should target).
    /// </summary>
    private void OnFocusedDockableChanged(object? sender, FocusedDockableChangedEventArgs e)
    {
        if (e.Dockable is DiagramDocumentViewModel diagram)
        {
            _shell.NotifyActiveDiagramTab(diagram.TabId);
        }
    }

    /// <summary>
    ///     Handles a diagram document closing through Dock's own chrome by retiring the corresponding tab from
    ///     the shell and evicting the tracked view model.
    /// </summary>
    private void OnDiagramTabClosed(object? sender, DiagramDocumentViewModel diagram)
    {
        _shell.CloseDiagramTab(diagram.TabId);
        _tabViewModelsByTabId.Remove(diagram.TabId);
    }

    /// <summary>
    ///     Handles a source-text document closing through Dock's own chrome by retiring the corresponding tab
    ///     from the shell and evicting the tracked view model. Calls the same <see cref="MainWindowShell.CloseDiagramTab" />
    ///     as <see cref="OnDiagramTabClosed" /> - that method already operates generically on any
    ///     <c>WorkbenchTab.Id</c> regardless of kind, so no separate shell method is needed for this tab kind.
    /// </summary>
    private void OnSourceTextTabClosed(object? sender, SourceTextDocumentViewModel sourceText)
    {
        _shell.CloseDiagramTab(sourceText.TabId);
        _tabViewModelsByTabId.Remove(sourceText.TabId);
    }

    /// <summary>
    ///     Reconciles the Dock <see cref="WorkbenchDockFactory.DiagramDock" /> against
    ///     <see cref="MainWindowShell.OpenTabs" />: creates a document for every newly opened tab, removes the
    ///     document for every tab that no longer exists (covers <c>ApplyWorkspaceSnapshot</c>'s tab-clear-on-reload
    ///     path, which bypasses <see cref="MainWindowShell.CloseDiagramTab" />), repaints and retitles the active
    ///     tab's document, and makes it the active/focused dockable so the user sees whatever tab the shell just
    ///     opened, closed, or updated.
    /// </summary>
    private void OnShellTabsChanged(object? sender, EventArgs e)
    {
        var openTabIds = _shell.OpenTabs.Select(tab => tab.Id).ToHashSet();

        // Add documents for newly opened tabs.
        foreach (var tab in _shell.OpenTabs)
        {
            if (_tabViewModelsByTabId.ContainsKey(tab.Id))
            {
                continue;
            }

            Dock.Model.Mvvm.Controls.Document tabViewModel = tab.Kind == WorkbenchTabKind.SourceText
                ? new SourceTextDocumentViewModel(_shell, tab.Id) { Id = tab.Id, Title = tab.Title }
                : new DiagramDocumentViewModel(_shell, tab.Id) { Id = tab.Id, Title = tab.Title };
            _tabViewModelsByTabId[tab.Id] = tabViewModel;
            _dockFactory.AddDockable(_dockFactory.DiagramDock, tabViewModel);
        }

        // Remove documents for tabs that no longer exist (for example, a workspace reload cleared every tab).
        foreach (var staleTabId in _tabViewModelsByTabId.Keys.Where(tabId => !openTabIds.Contains(tabId)).ToList())
        {
            _dockFactory.RemoveDockable(_tabViewModelsByTabId[staleTabId], false);
            _tabViewModelsByTabId.Remove(staleTabId);
        }

        // Refresh the active tab's title and repaint it in place.
        if (_shell.ActiveTabId is { } activeTabId && _tabViewModelsByTabId.TryGetValue(activeTabId, out var activeViewModel))
        {
            var activeTab = _shell.OpenTabs.First(tab => tab.Id == activeTabId);
            activeViewModel.Title = activeTab.Title;

            if (activeViewModel is DiagramDocumentViewModel diagram)
            {
                diagram.RaiseDiagramChanged();
            }

            _dockFactory.SetActiveDockable(activeViewModel);
            _dockFactory.SetFocusedDockable(_dockFactory.DiagramDock, activeViewModel);
        }
    }

    private async void OnAddFileSourceClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open SysML2 File",
                AllowMultiple = false,
                FileTypeFilter = [SysmlFileType],
            });

            var filePath = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            await _shell.AddFileSourceAsync(filePath);
            RefreshPanelsFromWorkspace();
        }
        catch (Exception ex)
        {
            _workspacePanelViewModel.StatusMessage = $"Failed to open file: {ex.Message}";
        }
    }

    private async void OnAddFolderSourceClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        try
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open SysML2 Folder",
                AllowMultiple = false,
            });

            var folderPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            await _shell.AddFolderSourceAsync(folderPath);
            RefreshPanelsFromWorkspace();
        }
        catch (Exception ex)
        {
            _workspacePanelViewModel.StatusMessage = $"Failed to open folder: {ex.Message}";
        }
    }

    /// <summary>
    ///     Handles a drag-and-drop drop anywhere on the main window by adding each dropped file or folder as a
    ///     new workspace source, funneling through the same <see cref="MainWindowShell" /> APIs the File menu and
    ///     Workspace panel use - no separate drag-and-drop orchestration.
    /// </summary>
    private async void OnWindowDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            return;
        }

        foreach (var item in e.DataTransfer.TryGetFiles() ?? [])
        {
            var path = item.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    await _shell.AddFolderSourceAsync(path);
                }
                else if (File.Exists(path))
                {
                    await _shell.AddFileSourceAsync(path);
                }
            }
            catch (Exception ex)
            {
                _workspacePanelViewModel.StatusMessage = $"Failed to open dropped path '{path}': {ex.Message}";
            }
        }

        RefreshPanelsFromWorkspace();
    }

    private void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    /// <summary>
    ///     Refreshes the two workspace-derived tool panels after a source is added or removed through the File
    ///     menu or a drag-and-drop drop. The Workspace panel itself refreshes independently via its own
    ///     <see cref="MainWindowShell.SourcesChanged" /> subscription.
    /// </summary>
    private void RefreshPanelsFromWorkspace()
    {
        _predefinedViewsViewModel.RefreshFromWorkspace();
        _diagnosticsViewModel.RefreshFromWorkspace();
    }
}
