namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Seam for writing text to the OS clipboard, so view models that need to copy generated text (for example a
///     diagram tab's "Copy as SysML" action) can be exercised in unit tests with a fake implementation rather than
///     requiring a live Avalonia <see cref="Avalonia.Input.Platform.IClipboard" />/<c>TopLevel</c>. Mirrors the
///     existing <see cref="DemaConsulting.SysML2Workbench.WorkspaceSubsystem.IUiDispatcher" /> seam pattern: a small
///     interface owned alongside its real implementation, with production wiring supplying the live implementation
///     and tests supplying a lightweight double.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    ///     Writes <paramref name="text" /> to the OS clipboard.
    /// </summary>
    /// <param name="text">Text to place on the clipboard.</param>
    /// <returns>A task that completes once the clipboard write has finished.</returns>
    Task SetTextAsync(string text);
}
