using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     <see cref="IClipboardService" /> implementation that writes to the live Avalonia clipboard, resolved via
///     <see cref="TopLevel.GetTopLevel(Visual)" /> from a control anchored in the visual tree.
/// </summary>
/// <remarks>
///     The owning <see cref="TopLevel" /> is intentionally resolved fresh on every <see cref="SetTextAsync" /> call
///     rather than cached at construction, since the anchor control may not yet be attached to a window at the
///     point this service is constructed (for example, if a diagram document view model is bound before its view
///     is attached to the Dock layout) - by the time a user actually triggers a clipboard copy, the control is
///     expected to be attached, but resolving lazily is a safe, zero-cost precaution either way.
/// </remarks>
public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly Visual _anchor;
    private readonly RollingFileLogger? _logger;

    /// <summary>
    ///     Creates a clipboard service anchored to a control in the visual tree.
    /// </summary>
    /// <param name="anchor">Control used to resolve the owning <see cref="TopLevel" /> at write time.</param>
    /// <param name="logger">
    ///     Optional logger used to record the case where no <see cref="TopLevel" />/clipboard is available (for
    ///     example under the XAML designer). When omitted, that case is silently ignored rather than throwing.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="anchor" /> is null.</exception>
    public AvaloniaClipboardService(Visual anchor, RollingFileLogger? logger = null)
    {
        _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var clipboard = TopLevel.GetTopLevel(_anchor)?.Clipboard;
        if (clipboard is null)
        {
            _logger?.Log(LogLevel.Error, "Unable to copy to the clipboard: no Avalonia TopLevel/clipboard is available.");
            return;
        }

        await clipboard.SetTextAsync(text);
    }
}
