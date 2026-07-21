using Avalonia.Controls;
using Avalonia.Interactivity;
using DemaConsulting.SysML2Tools.Query;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the modal Query dialog. Owns the Avalonia clipboard-service
///     assignment (mirroring <see cref="DiagramDocumentView" />'s unconditional
///     <c>OnDataContextChanged</c> pattern so the same clipboard-plumbing rules apply here) and the
///     Query-Type-specific control visibility toggles that would be awkward to express as pure XAML data
///     triggers in Avalonia 11. This single-form design has no "Run" button and no toolbar: every
///     interaction recomputes the view model's result synchronously, and the results panel's copy
///     actions live entirely on its right-click <c>ContextMenu</c>.
/// </summary>
public partial class QueryDialogView : Window
{
    private QueryDialogViewModel? _viewModel;

    /// <summary>
    ///     Parameterless constructor required by the Avalonia XAML previewer/designer. Not used at
    ///     runtime.
    /// </summary>
    public QueryDialogView()
        : this(DesignTimeShellFactory.Create())
    {
    }

    /// <summary>
    ///     Creates the dialog, constructing a fresh <see cref="QueryDialogViewModel" /> as its data
    ///     context. Used both at runtime (by <see cref="MainWindowView" />'s Query menu handler) and by
    ///     the Avalonia XAML previewer/designer.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public QueryDialogView(MainWindowShell shell)
    {
        InitializeComponent();

        // Populate the Query Type / direction combo boxes from the view-model's canonical option lists
        // so the AXAML markup stays independent of the QueryVerb enum's declaration order and the
        // hierarchy-direction string vocabulary.
        QueryTypeComboBox.ItemsSource = QueryDialogViewModel.QueryTypes;
        HierarchyDirectionComboBox.ItemsSource = QueryDialogViewModel.HierarchyDirectionOptions;

        QueryTypeComboBox.SelectionChanged += OnQueryTypeSelectionChanged;
        HierarchyDirectionComboBox.SelectionChanged += OnHierarchyDirectionSelectionChanged;

        DataContext = new QueryDialogViewModel(shell);
        AttachViewModel();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    ///     Re-anchors the view's dependencies (VM back-reference, clipboard service, Query
    ///     Type/direction combos, initial results-panel state) whenever the <c>DataContext</c> is
    ///     reassigned. Follows <see cref="DiagramDocumentView" />'s "always rebind" (unconditional, no
    ///     <c>??=</c>) pattern so a re-shown dialog never leaves the clipboard service pointing at a
    ///     stale window instance.
    /// </summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        AttachViewModel();
    }

    /// <summary>
    ///     Wires the current <c>DataContext</c> as the active view model: subscribes to its
    ///     <c>PropertyChanged</c> stream so Query-Type-visibility and results-panel visibility stay in
    ///     sync (copy-menu-item enablement is handled purely by the <c>HasCurrentResult</c> binding);
    ///     primes the initial UI from the VM's current state; and unconditionally assigns a fresh
    ///     <see cref="AvaloniaClipboardService" /> so the context-menu copy actions write to this
    ///     window's clipboard.
    /// </summary>
    private void AttachViewModel()
    {
        _viewModel = DataContext as QueryDialogViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        QueryTypeComboBox.SelectedItem = _viewModel.SelectedQueryType;
        HierarchyDirectionComboBox.SelectedItem = _viewModel.HierarchyDirection;

        // See DiagramDocumentView.OnDataContextChanged for the "always rebind (unconditional, no `??=`)"
        // rationale: a subsequent DataContext reassignment (or a re-shown dialog) must anchor the
        // clipboard service to THIS window instance so TopLevel.GetTopLevel(this) resolves live.
        _viewModel.ClipboardService = new AvaloniaClipboardService(this);

        ApplyQueryTypeVisibility();
        ApplyResultVisibility();
    }

    /// <summary>
    ///     Observes the view model's property changes and drives the code-behind's visibility toggles:
    ///     Query Type changes retarget which per-verb controls are visible, and current result / result
    ///     rows changes toggle the results panel's visibility.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(QueryDialogViewModel.SelectedQueryType):
                if (_viewModel is not null)
                {
                    QueryTypeComboBox.SelectedItem = _viewModel.SelectedQueryType;
                }

                ApplyQueryTypeVisibility();
                ApplyResultVisibility();
                break;

            case nameof(QueryDialogViewModel.HierarchyDirection):
                if (_viewModel is not null)
                {
                    HierarchyDirectionComboBox.SelectedItem = _viewModel.HierarchyDirection;
                }

                break;

            case nameof(QueryDialogViewModel.CurrentResult):
            case nameof(QueryDialogViewModel.CurrentResultRows):
                ApplyResultVisibility();
                break;
        }
    }

    /// <summary>
    ///     Toggles the Hierarchy / Impact per-verb control panels' visibility based on the currently
    ///     selected Query Type.
    /// </summary>
    private void ApplyQueryTypeVisibility()
    {
        if (_viewModel is null)
        {
            return;
        }

        HierarchyDirectionPanel.IsVisible = _viewModel.SelectedQueryType == QueryVerb.Hierarchy;
        WalkDepthPanel.IsVisible = _viewModel.SelectedQueryType == QueryVerb.Impact;
    }

    /// <summary>
    ///     Toggles the results panel's visibility based on the current
    ///     <see cref="QueryDialogViewModel.CurrentResult" />. Copy-menu-item enablement is handled purely
    ///     by the AXAML's <c>IsEnabled="{Binding HasCurrentResult}"</c> bindings, so this method no longer
    ///     needs to imperatively toggle any button/menu-item state (unlike the old toolbar-button design).
    /// </summary>
    private void ApplyResultVisibility()
    {
        if (_viewModel is null)
        {
            return;
        }

        var result = _viewModel.CurrentResult;
        var hasEntries = _viewModel.CurrentResultRows.Count > 0;

        // Summary lines: bind directly (rather than through a converter) so unit tests that only touch
        // the VM don't need to spin up an Avalonia visual tree.
        SummaryItemsControl.ItemsSource = result?.Summary;
        EntriesTableBorder.IsVisible = hasEntries;

        // The Direction column is dependency-verb specific per the plan; keep the header in lockstep
        // with the row-template columns by only showing it when the current result is a dependency-verb
        // result.
        DirectionHeaderTextBlock.IsVisible = string.Equals(result?.Verb, "dependencies", StringComparison.Ordinal);
    }

    private void OnQueryTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is not null && QueryTypeComboBox.SelectedItem is QueryVerb queryType)
        {
            _viewModel.SelectedQueryType = queryType;
        }
    }

    private void OnHierarchyDirectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is not null && HierarchyDirectionComboBox.SelectedItem is string direction)
        {
            _viewModel.HierarchyDirection = direction;
        }
    }

    private async void OnCopyAsMarkdownMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.CopyResultAsMarkdownAsync();
    }

    private async void OnCopyAsJsonMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.CopyResultAsJsonAsync();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
