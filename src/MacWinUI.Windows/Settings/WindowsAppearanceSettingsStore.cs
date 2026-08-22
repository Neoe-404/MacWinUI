using System.IO;
using System.Text.Json;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Settings;

public sealed class WindowsAppearanceSettingsStore(
    ILogger<WindowsAppearanceSettingsStore> logger) : IAppearanceSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MacWinUI",
        "appearance.json");

    public async Task<DockAppearanceSnapshot?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var snapshot = await JsonSerializer.DeserializeAsync<DockAppearanceSnapshot>(
                stream,
                SerializerOptions,
                cancellationToken);
            if (snapshot is not null
                && snapshot.SchemaVersion > DockAppearanceSnapshot.CurrentSchemaVersion)
            {
                logger.LogWarning(
                    "Appearance settings schema {SchemaVersion} is newer than supported schema {SupportedSchemaVersion}; defaults will be used.",
                    snapshot.SchemaVersion,
                    DockAppearanceSnapshot.CurrentSchemaVersion);
                return null;
            }

            return snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Appearance settings are malformed; defaults will be used.");
            BackupBrokenSettings();
            return null;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not load appearance settings; defaults will be used.");
            return null;
        }
    }

    private void BackupBrokenSettings()
    {
        var backupPath = $"{_settingsPath}.broken";
        try
        {
            File.Move(_settingsPath, backupPath, overwrite: true);
            logger.LogInformation(
                "Malformed appearance settings were moved to {BackupPath}.",
                backupPath);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not back up malformed appearance settings.");
        }
    }

    public async Task SaveAsync(
        DockAppearanceSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Appearance settings path has no directory.");
        var temporaryPath = $"{_settingsPath}.tmp";

        try
        {
            Directory.CreateDirectory(directory);
            if (File.Exists(_settingsPath))
            {
                File.Copy(_settingsPath, $"{_settingsPath}.backup", overwrite: true);
            }
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    snapshot,
                    SerializerOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not save appearance settings.");
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception) when (
                exception is IOException
                or UnauthorizedAccessException)
            {
                logger.LogDebug(exception, "Could not remove the temporary appearance settings file.");
            }
        }
    }
}
