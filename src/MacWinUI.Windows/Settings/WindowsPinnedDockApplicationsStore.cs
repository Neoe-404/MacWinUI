using System.IO;
using System.Text.Json;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Settings;

public sealed class WindowsPinnedDockApplicationsStore(
    ILogger<WindowsPinnedDockApplicationsStore> logger) : IPinnedDockApplicationsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MacWinUI",
        "dock-apps.json");

    public async Task<PinnedDockApplicationsSnapshot?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            var snapshot = await JsonSerializer.DeserializeAsync<PinnedDockApplicationsSnapshot>(
                stream,
                SerializerOptions,
                cancellationToken);
            if (snapshot is not null
                && snapshot.SchemaVersion > PinnedDockApplicationsSnapshot.CurrentSchemaVersion)
            {
                logger.LogWarning(
                    "Pinned Dock applications schema {SchemaVersion} is newer than supported schema {SupportedSchemaVersion}; custom applications will not be loaded.",
                    snapshot.SchemaVersion,
                    PinnedDockApplicationsSnapshot.CurrentSchemaVersion);
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
            logger.LogWarning(exception, "Pinned Dock applications are malformed and will not be loaded.");
            BackupBrokenSettings();
            return null;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not load pinned Dock applications.");
            return null;
        }
    }

    public async Task SaveAsync(
        PinnedDockApplicationsSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Pinned Dock applications path has no directory.");
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
            logger.LogWarning(exception, "Could not save pinned Dock applications.");
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
                logger.LogDebug(exception, "Could not remove the temporary pinned Dock applications file.");
            }
        }
    }

    private void BackupBrokenSettings()
    {
        var backupPath = $"{_settingsPath}.broken";
        try
        {
            File.Move(_settingsPath, backupPath, overwrite: true);
            logger.LogInformation(
                "Malformed pinned Dock applications were moved to {BackupPath}.",
                backupPath);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not back up malformed pinned Dock applications.");
        }
    }
}
