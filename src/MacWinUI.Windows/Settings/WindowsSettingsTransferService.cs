using System.IO;
using System.Text.Json;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;

namespace MacWinUI.Windows.Settings;

public sealed class WindowsSettingsTransferService : ISettingsTransferService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task ExportAsync(
        string path,
        MacWinUISettingsBundle bundle,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bundle);
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true);
        await JsonSerializer.SerializeAsync(stream, bundle, Options, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public async Task<MacWinUISettingsBundle?> ImportAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = File.OpenRead(path);
        var bundle = await JsonSerializer.DeserializeAsync<MacWinUISettingsBundle>(
            stream,
            Options,
            cancellationToken);
        return bundle is not null
               && bundle.SchemaVersion <= MacWinUISettingsBundle.CurrentSchemaVersion
            ? bundle
            : null;
    }
}
