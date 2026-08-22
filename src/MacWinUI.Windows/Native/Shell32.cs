using System.Runtime.InteropServices;

namespace MacWinUI.Windows.Native;

internal static class Shell32
{
    internal const uint AppBarMessageNew = 0x00000000;
    internal const uint AppBarMessageRemove = 0x00000001;
    internal const uint AppBarMessageQueryPosition = 0x00000002;
    internal const uint AppBarMessageSetPosition = 0x00000003;
    internal const uint AppBarEdgeTop = 1;
    internal const uint FileInfoIcon = 0x00000100;
    internal const uint FileInfoLargeIcon = 0x00000000;
    internal const uint FileInfoShellIconSize = 0x00000004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern nuint SHGetFileInfo(
        string path,
        uint fileAttributes,
        ref ShellFileInfo fileInfo,
        uint fileInfoSize,
        uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint ExtractIconEx(
        string fileName,
        int iconIndex,
        nint[] largeIconHandles,
        nint[] smallIconHandles,
        uint iconCount);

    [DllImport("shell32.dll")]
    internal static extern nuint SHAppBarMessage(
        uint message,
        ref AppBarData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    internal static extern void SHCreateItemFromParsingName(
        string path,
        nint bindingContext,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory imageFactory);

    [ComImport]
    [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItemImageFactory
    {
        void GetImage(
            NativeSize size,
            ShellItemImageFlags flags,
            out nint bitmapHandle);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct NativeSize(int width, int height)
    {
        internal readonly int Width = width;
        internal readonly int Height = height;
    }

    [Flags]
    internal enum ShellItemImageFlags
    {
        ResizeToFit = 0,
        BiggerSizeOk = 1,
        IconOnly = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AppBarData
    {
        internal uint Size;
        internal nint WindowHandle;
        internal uint CallbackMessage;
        internal uint Edge;
        internal AppBarRect Rect;
        internal nint Parameter;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AppBarRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ShellFileInfo
    {
        internal nint IconHandle;
        internal int IconIndex;
        internal uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        internal string TypeName;
    }
}
