using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Convention-based Dock view locator (Dock's MVVM guide "Option B"). Maps a dockable view model whose type
///     name ends in <c>ViewModel</c> to a same-namespace, same-assembly control type with the same name but
///     ending in <c>View</c> instead, avoiding a source-generator dependency for this one-to-one mapping.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        var type = ResolveViewType(data.GetType());
        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + data.GetType().FullName };
    }

    /// <inheritdoc />
    public bool Match(object? data)
    {
        if (data is null)
        {
            return false;
        }

        return ResolveViewType(data.GetType()) is not null;
    }

    private static Type? ResolveViewType(Type viewModelType)
    {
        var fullName = viewModelType.FullName;
        if (fullName is null || !fullName.EndsWith("ViewModel", StringComparison.Ordinal))
        {
            return null;
        }

        var viewName = fullName[..^"ViewModel".Length] + "View";
        var assemblyQualifiedName = $"{viewName}, {viewModelType.Assembly.FullName}";
        var type = Type.GetType(assemblyQualifiedName);

        return type is not null && typeof(Control).IsAssignableFrom(type) ? type : null;
    }
}
