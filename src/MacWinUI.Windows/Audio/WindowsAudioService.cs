using System.Runtime.InteropServices;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Core.System;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Audio;

public sealed class WindowsAudioService(
    ILogger<WindowsAudioService> logger) : IAudioService
{
    public async Task<AudioState> GetStateAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(
                () => ReadState(cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read the default Windows audio endpoint.");
            return default;
        }
    }

    public async Task SetVolumeAsync(
        double volumePercent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(
                () => WithDefaultEndpoint(
                    endpoint =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var eventContext = Guid.Empty;
                        Marshal.ThrowExceptionForHR(
                            endpoint.SetMasterVolumeLevelScalar(
                                AudioVolume.PercentToScalar(volumePercent),
                                eventContext));
                        return true;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not set the default Windows audio volume.");
        }
    }

    public async Task SetMutedAsync(
        bool isMuted,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(
                () => WithDefaultEndpoint(
                    endpoint =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var eventContext = Guid.Empty;
                        Marshal.ThrowExceptionForHR(endpoint.SetMute(isMuted, eventContext));
                        return true;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not change the default Windows audio mute state.");
        }
    }

    private static AudioState ReadState(CancellationToken cancellationToken)
    {
        return WithDefaultEndpoint(
            endpoint =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out var scalar));
                Marshal.ThrowExceptionForHR(endpoint.GetMute(out var isMuted));
                return new AudioState(
                    true,
                    AudioVolume.ScalarToPercent(scalar),
                    isMuted);
            });
    }

    private static T WithDefaultEndpoint<T>(
        Func<CoreAudioInterop.IAudioEndpointVolume, T> operation)
    {
        object? enumeratorObject = null;
        CoreAudioInterop.IMultimediaDevice? device = null;
        object? endpointObject = null;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(
                CoreAudioInterop.MultimediaDeviceEnumeratorClassId,
                throwOnError: true)!;
            enumeratorObject = Activator.CreateInstance(enumeratorType)
                ?? throw new InvalidOperationException("Could not create the Windows audio enumerator.");
            var enumerator = (CoreAudioInterop.IMultimediaDeviceEnumerator)enumeratorObject;

            Marshal.ThrowExceptionForHR(
                enumerator.GetDefaultAudioEndpoint(
                    CoreAudioInterop.DataFlow.Render,
                    CoreAudioInterop.Role.Multimedia,
                    out device));

            var interfaceId = CoreAudioInterop.AudioEndpointVolumeInterfaceId;
            Marshal.ThrowExceptionForHR(
                device.Activate(
                    ref interfaceId,
                    CoreAudioInterop.ClassContextAll,
                    0,
                    out endpointObject));

            return operation((CoreAudioInterop.IAudioEndpointVolume)endpointObject);
        }
        finally
        {
            ReleaseComObject(endpointObject);
            ReleaseComObject(device);
            ReleaseComObject(enumeratorObject);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
