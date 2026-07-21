using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the modal Custom View Builder dialog. Mutates
///     <see cref="ViewBuilderDialogViewModel.Definition" /> directly from per-row controls it builds by hand
///     (no data-template/binding framework), since each selected-target row needs individually addressable
///     recursion-kind and bracket-filter state that a flat "read all controls" rebuild cannot reconstruct -
///     the exact same rationale the deleted <c>CustomViewBuilderToolView</c> used for the same UI shape.
/// </summary>
public partial class ViewBuilderDialogView : Window
{
    /// <summary>
    ///     Human-readable display text for each <see cref="ExposeRecursionKind" />, shown in each selected
    ///     target's row <see cref="ComboBox" />, in declaration order.
    /// </summary>
    private static readonly (ExposeRecursionKind Kind, string Display)[] RecursionKindOptions =
    [
        (ExposeRecursionKind.MembershipExact, "This element only"),
        (ExposeRecursionKind.MembershipRecursive, "This element + everything below (::**)"),
        (ExposeRecursionKind.NamespaceDirectChildren, "Direct children only (::*)"),
        (ExposeRecursionKind.NamespaceRecursive, "All descendants, not itself (::*::**)"),
    ];

    private readonly ViewBuilderDialogViewModel _viewModel;

    /// <summary>
    ///     Parameterless constructor required by the Avalonia XAML previewer/designer. Not used at runtime.
    /// </summary>
    public ViewBuilderDialogView()
        : this(DesignTimeShellFactory.Create())
    {
    }

    /// <summary>
    ///     Creates the dialog, constructing a fresh <see cref="ViewBuilderDialogViewModel" /> as its data
    ///     context. Used both at runtime (by <see cref="MainWindowView" />'s View menu handler) and by the
    ///     Avalonia XAML previewer/designer.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public ViewBuilderDialogView(MainWindowShell shell)
    {
        InitializeComponent();

        _viewModel = new ViewBuilderDialogViewModel(shell);
        DataContext = _viewModel;

        ViewKindComboBox.ItemsSource = Enum.GetValues<ViewKind>();
        ViewKindComboBox.SelectionChanged += OnViewKindSelectionChanged;

        AddExposeTargetButton.Click += OnAddExposeTargetClick;

        AddExposeTypeFilterButton.Flyout!.Opened += OnAddExposeTypeFilterFlyoutOpening;
        AddableExposeTypeFilterListBox.SelectionChanged += OnAddableExposeTypeFilterSelectionChanged;

        FilterExpressionTextBox.LostFocus += OnFilterExpressionLostFocus;
        DisplayNameTextBox.LostFocus += OnDisplayNameLostFocus;

        _viewModel.PreviewChanged += OnPreviewChanged;

        RefreshSelectedExposeTargetsPanel();
        LoadPreviewImage();
    }

    private void OnViewKindSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewKindComboBox.SelectedItem is ViewKind viewKind)
        {
            _viewModel.SetViewKind(viewKind);
        }
    }

    private void OnFilterExpressionLostFocus(object? sender, RoutedEventArgs e)
    {
        _viewModel.SetFilterExpression(FilterExpressionTextBox.Text);
    }

    private void OnDisplayNameLostFocus(object? sender, RoutedEventArgs e)
    {
        _viewModel.SetDisplayName(DisplayNameTextBox.Text);
    }

    /// <summary>
    ///     Adds the currently-selected available target to the definition and refreshes the selected-targets
    ///     panel to show its new row.
    /// </summary>
    private void OnAddExposeTargetClick(object? sender, RoutedEventArgs e)
    {
        if (AvailableExposeTargetsListBox.SelectedItem is not string qualifiedName)
        {
            return;
        }

        _viewModel.AddExposeTarget(qualifiedName);
        RefreshSelectedExposeTargetsPanel();
    }

    private void OnPreviewChanged(object? sender, EventArgs e)
    {
        LoadPreviewImage();
    }

    /// <summary>
    ///     Populates <see cref="AddableExposeTypeFilterListBox" /> from the view model's currently addable type
    ///     labels each time the "+" button's flyout is about to open, so the list always reflects the latest
    ///     workspace/active-filter state rather than a stale snapshot from construction time.
    /// </summary>
    private void OnAddExposeTypeFilterFlyoutOpening(object? sender, EventArgs e)
    {
        AddableExposeTypeFilterListBox.ItemsSource = _viewModel.GetAddableExposeTargetTypeLabels();
    }

    /// <summary>
    ///     Adds the selected type label as a new active filter chip and closes the flyout.
    /// </summary>
    private void OnAddableExposeTypeFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AddableExposeTypeFilterListBox.SelectedItem is not string typeLabel)
        {
            return;
        }

        _viewModel.AddExposeTypeFilter(typeLabel);
        AddableExposeTypeFilterListBox.SelectedItem = null;
        AddExposeTypeFilterButton.Flyout?.Hide();
    }

    /// <summary>
    ///     Removes the clicked chip's type label from the active filters.
    /// </summary>
    private void OnRemoveExposeTypeFilterClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string typeLabel })
        {
            _viewModel.RemoveExposeTypeFilter(typeLabel);
        }
    }

    /// <summary>
    ///     Loads this dialog's currently rendered preview SVG into the on-screen image control.
    /// </summary>
    private void LoadPreviewImage()
    {
        if (_viewModel.PreviewCanvas.CurrentSvg is null)
        {
            PreviewImage.Source = null;
            return;
        }

        PreviewImage.Source = new SvgImage { Source = SvgSource.LoadFromSvg(_viewModel.PreviewCanvas.CurrentSvg) };
    }

    /// <summary>
    ///     Rebuilds the "Selected Targets" panel's rows from the definition's current expose targets. Each row
    ///     is constructed directly in code (no data-binding/template framework) so its recursion-kind
    ///     <see cref="ComboBox" />, bracket-filter <see cref="TextBox" />, and Remove <see cref="Button" /> can
    ///     close over the row's own qualified name and recursion kind and mutate the definition directly.
    /// </summary>
    private void RefreshSelectedExposeTargetsPanel()
    {
        SelectedExposeTargetsPanel.Children.Clear();

        foreach (var selection in _viewModel.Definition.ExposeTargets)
        {
            var qualifiedName = selection.QualifiedName;
            var currentKind = selection.RecursionKind;

            var nameText = new TextBlock { Text = qualifiedName, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap };

            var kindComboBox = new ComboBox
            {
                ItemsSource = RecursionKindOptions.Select(o => o.Display).ToList(),
                SelectedIndex = Array.FindIndex(RecursionKindOptions, o => o.Kind == selection.RecursionKind),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            kindComboBox.SelectionChanged += (_, _) =>
            {
                if (kindComboBox.SelectedIndex < 0)
                {
                    return;
                }

                _viewModel.SetExposeRecursionKind(qualifiedName, currentKind, RecursionKindOptions[kindComboBox.SelectedIndex].Kind);
                RefreshSelectedExposeTargetsPanel();
            };

            var removeButton = new Button { Content = "Remove" };
            removeButton.Click += (_, _) =>
            {
                _viewModel.RemoveExposeTarget(qualifiedName, currentKind);
                RefreshSelectedExposeTargetsPanel();
            };

            var kindRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            kindRow.Children.Add(kindComboBox);
            kindRow.Children.Add(removeButton);

            var isRecursive = selection.RecursionKind is ExposeRecursionKind.MembershipRecursive or ExposeRecursionKind.NamespaceRecursive;
            var filterTextBox = new TextBox
            {
                PlaceholderText = "Bracket filter (optional)",
                Text = selection.BracketFilterExpression,
                IsEnabled = isRecursive,
            };
            ToolTip.SetTip(filterTextBox, "Narrows this target's expose::** / ::*::** membership to elements matching a SysML v2 filter expression (Phase 1 subset). Only applies to the two recursive recursion kinds.");
            filterTextBox.LostFocus += (_, _) => _viewModel.SetExposeBracketFilter(qualifiedName, currentKind, filterTextBox.Text);

            var row = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Child = new StackPanel
                {
                    Spacing = 2,
                    Children = { nameText, kindRow, filterTextBox },
                },
            };

            SelectedExposeTargetsPanel.Children.Add(row);
        }
    }

    /// <summary>
    ///     Handles the OK button click by committing the current definition as a brand-new diagram tab
    ///     (<see cref="ViewBuilderDialogViewModel.TryCommit" />). On success the dialog closes; on failure the
    ///     dialog stays open with <see cref="ViewBuilderDialogViewModel.StatusMessage" /> already set, so a
    ///     validation error never silently discards the user's in-progress definition.
    /// </summary>
    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.TryCommit(out _))
        {
            Close();
        }
    }

    /// <summary>
    ///     Handles the Cancel button click by closing the dialog. Makes zero calls into <see cref="MainWindowShell" />
    ///     - nothing was ever created or mutated on the shell side because live preview only ever touched the
    ///     dialog's own <see cref="ViewBuilderDialogViewModel.PreviewCanvas" />.
    /// </summary>
    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
