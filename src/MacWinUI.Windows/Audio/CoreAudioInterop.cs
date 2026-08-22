using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Audio;

internal static class CoreAudioInterop
{
    internal static readonly Guid MultimediaDeviceEnumeratorClassId =
        new("BCDE0395-E52F-467C-8E3D-C4579291692E");

    internal static readonly Guid AudioEndpointVolumeInterfaceId =
        new("5CDF2C82-841E-4546-9722-0CF74078229A");

    internal const uint ClassContextAll = 23;

    internal enum DataFlow
    {
        Render,
        Capture,
        All
    }

    internal enum Role
    {
        Console,
        Multimedia,
        Communications
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMultimediaDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(
            DataFlow dataFlow,
            uint stateMask,
            out nint devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(
            DataFlow dataFlow,
            Role role,
            out IMultimediaDevice device);

        [PreserveSig]
        int GetDevice(
            [MarshalAs(UnmanagedType.LPWStr)] string id,
            out IMultimediaDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(nint client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(nint client);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMultimediaDevice
    {
        [PreserveSig]
        int Activate(
            ref Guid interfaceId,
            uint classContext,
            nint activationParameters,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfaceObject);

        [PreserveSig]
        int OpenPropertyStore(uint storageAccessMode, out nint properties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        [PreserveSig]
        int GetState(out uint state);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(nint notify);

        [PreserveSig]
        int UnregisterControlChangeNotify(nint notify);

        [PreserveSig]
        int GetChannelCount(out uint channelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float levelDb, in Guid eventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float level, in Guid eventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float levelDb);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float level);

        [PreserveSig]
        int SetChannelVolumeLevel(uint channel, float levelDb, in Guid eventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint channel, float level, in Guid eventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint channel, out float levelDb);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint channel, out float level);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool isMuted, in Guid eventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);

        [PreserveSig]
        int GetVolumeStepInfo(out uint step, out uint stepCount);

        [PreserveSig]
        int VolumeStepUp(in Guid eventContext);

        [PreserveSig]
        int VolumeStepDown(in Guid eventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint hardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float minimumDb, out float maximumDb, out float incrementDb);
    }
}
