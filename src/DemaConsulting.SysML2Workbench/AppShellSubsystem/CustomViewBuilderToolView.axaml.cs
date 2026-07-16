using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for the "Custom View Builder" Dock tool panel. Mutates
///     <see cref="CustomViewBuilderToolViewModel.BuilderDefinition" /> directly from per-row controls it builds by
///     hand (no data-template/binding framework), since each selected-target row needs individually addressable
///     recursion-kind and bracket-filter state that a flat "read all controls" rebuild cannot reconstruct.
/// </summary>
public partial class CustomViewBuilderToolView : UserControl
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

    private CustomViewBuilderToolViewModel? _viewModel;

    /// <summary>
    ///     Constructor used both at runtime (by Dock's view locator) and by the Avalonia XAML previewer/designer.
    /// </summary>
    public CustomViewBuilderToolView()
    {
        InitializeComponent();

        ViewKindComboBox.ItemsSource = Enum.GetValues<ViewKind>();

        AddExposeTargetButton.Click += OnAddExposeTargetClick;
        NewDiagramTabButton.Click += OnNewDiagramTabClick;
        PreviewCustomViewButton.Click += OnPreviewCustomViewClick;
        CopyAsSysmlButton.Click += OnCopyAsSysmlClick;

        DataContextChanged += OnDataContextChanged;

        if (Design.IsDesignMode)
        {
            var shell = DesignTimeShellFactory.Create();
            DataContext = new CustomViewBuilderToolViewModel(shell);
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.BuilderReset -= OnBuilderReset;
        }

        _viewModel = DataContext as CustomViewBuilderToolViewModel;

        if (_viewModel is not null)
        {
            _viewModel.BuilderReset += OnBuilderReset;
            RefreshSelectedExposeTargetsPanel();
        }
    }

    private void OnBuilderReset(object? sender, EventArgs e)
    {
        RefreshSelectedExposeTargetsPanel();
    }

    /// <summary>
    ///     Adds the currently-selected available target to the long-lived custom-view builder state and
    ///     refreshes the selected-targets panel to show its new row.
    /// </summary>
    private void OnAddExposeTargetClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null || AvailableExposeTargetsListBox.SelectedItem is not string qualifiedName)
        {
            return;
        }

        _viewModel.BuilderDefinition.AddExposeTarget(qualifiedName);
        RefreshSelectedExposeTargetsPanel();
    }

    private void OnPreviewCustomViewClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        try
        {
            var definition = BuildDefinitionFromBuilderControls();
            _viewModel.Shell.PreviewCustomView(definition);
            _viewModel.StatusMessage = null;
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Preview failed: {ex.Message}";
        }
    }

    /// <summary>
    ///     Opens a brand-new, empty custom-view-preview diagram tab and makes it active, so a subsequent
    ///     "Preview" click renders into it in place rather than opening yet another tab.
    /// </summary>
    private void OnNewDiagramTabClick(object? sender, RoutedEventArgs e)
    {
        _viewModel?.Shell.OpenNewCustomPreviewTab();
    }

    private async void OnCopyAsSysmlClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        try
        {
            var definition = BuildDefinitionFromBuilderControls();
            var snippet = _viewModel.Shell.ExportCustomViewSnippet(definition);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not null)
            {
                await topLevel.Clipboard.SetTextAsync(snippet);
            }

            _viewModel.StatusMessage = "Copied SysML snippet to clipboard.";
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    /// <summary>
    ///     Rebuilds the "Selected Targets" panel's rows from the builder definition's current expose targets.
    ///     Each row is constructed directly in code (no data-binding/template framework) so its recursion-kind
    ///     <see cref="ComboBox" />, bracket-filter <see cref="TextBox" />, and Remove <see cref="Button" /> can
    ///     close over the row's own qualified name and recursion kind and mutate the builder definition directly.
    /// </summary>
    private void RefreshSelectedExposeTargetsPanel()
    {
        SelectedExposeTargetsPanel.Children.Clear();

        if (_viewModel is null)
        {
            return;
        }

        var builderDefinition = _viewModel.BuilderDefinition;

        foreach (var selection in builderDefinition.ExposeTargets)
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

                builderDefinition.SetExposeRecursionKind(qualifiedName, currentKind, RecursionKindOptions[kindComboBox.SelectedIndex].Kind);
                RefreshSelectedExposeTargetsPanel();
            };

            var removeButton = new Button { Content = "Remove" };
            removeButton.Click += (_, _) =>
            {
                builderDefinition.RemoveExposeTarget(qualifiedName, currentKind);
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
            filterTextBox.LostFocus += (_, _) => builderDefinition.SetExposeBracketFilter(qualifiedName, currentKind, filterTextBox.Text);

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
    ///     Builds a <see cref="ViewDefinitionModel" /> from the current custom-view builder control values.
    /// </summary>
    /// <returns>Normalized custom-view state.</returns>
    private ViewDefinitionModel BuildDefinitionFromBuilderControls()
    {
        var builderDefinition = _viewModel!.BuilderDefinition;

        if (ViewKindComboBox.SelectedItem is ViewKind viewKind)
        {
            builderDefinition.SetViewKind(viewKind);
        }

        builderDefinition.SetFilterExpression(FilterExpressionTextBox.Text);
        builderDefinition.SetDisplayName(DisplayNameTextBox.Text);

        return builderDefinition;
    }
}
