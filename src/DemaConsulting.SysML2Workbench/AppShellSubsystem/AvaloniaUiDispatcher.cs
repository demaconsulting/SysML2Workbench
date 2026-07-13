using Avalonia.Threading;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     <see cref="IUiDispatcher" /> implementation that marshals callbacks onto the live Avalonia UI thread.
/// </summary>
/// <remarks>
///     This is thin, non-branching framework glue over <see cref="Dispatcher.UIThread" /> and is exercised only
///     by running the application; <see cref="FileWatcher" />'s own behavior is fully covered using
///     <see cref="ImmediateUiDispatcher" /> in unit tests, per that unit's documented test seam.
/// </remarks>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.UIThread.Post(action);
    }
}
