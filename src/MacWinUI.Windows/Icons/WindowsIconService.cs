using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Windows.Native;
using Microsoft.Extensions.Logging;

namespace MacWinUI.Windows.Icons;

public sealed class WindowsIconService(
    ILogger<WindowsIconService> logger) : IIconService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<byte[]?>>> _iconCache =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<byte[]?> GetIconPngAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var sourcePath = item.IconSourcePath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        sourcePath = Path.GetFullPath(sourcePath);
        var cacheKey = CreateCacheKey(sourcePath);

        var lazyIcon = _iconCache.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<byte[]?>>(
                () => Task.Run(() => ExtractIconPng(sourcePath)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var iconPng = await lazyIcon.Value
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (iconPng is null)
            {
                _iconCache.TryRemove(cacheKey, out _);
                logger.LogInformation(
                    "Windows Shell did not return an icon for dock item {DockItemId} at {IconSourcePath}; the stable fallback glyph will be used.",
                    item.Id,
                    sourcePath);
            }

            return iconPng;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not extract the icon for dock item {DockItemId}.",
                item.Id);
            _iconCache.TryRemove(cacheKey, out _);
            return null;
        }
    }

    private static string CreateCacheKey(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            return $"{path}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            return path;
        }
    }

    private static byte[]? ExtractIconPng(string path)
    {
        var shellImage = ExtractShellImagePng(path);
        if (shellImage is not null)
        {
            return shellImage;
        }

        var fileInfo = new Shell32.ShellFileInfo();
        var result = Shell32.SHGetFileInfo(
            path,
            0,
            ref fileInfo,
            (uint)Marshal.SizeOf<Shell32.ShellFileInfo>(),
            Shell32.FileInfoIcon
            | Shell32.FileInfoLargeIcon
            | Shell32.FileInfoShellIconSize);

        if (result != 0 && fileInfo.IconHandle != 0)
        {
            return EncodeAndDestroyIcon(fileInfo.IconHandle);
        }

        return ExtractEmbeddedIconPng(path);
    }

    private static byte[]? ExtractShellImagePng(string path)
    {
        Shell32.IShellItemImageFactory? imageFactory = null;
        nint bitmapHandle = 0;
        try
        {
            var interfaceId = typeof(Shell32.IShellItemImageFactory).GUID;
            Shell32.SHCreateItemFromParsingName(
                path,
                nint.Zero,
                ref interfaceId,
                out imageFactory);
            imageFactory.GetImage(
                new Shell32.NativeSize(256, 256),
                Shell32.ShellItemImageFlags.IconOnly
                | Shell32.ShellItemImageFlags.BiggerSizeOk,
                out bitmapHandle);
            if (bitmapHandle == 0)
            {
                return null;
            }

            var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmapHandle,
                nint.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            imageSource.Freeze();
            return EncodeImageSource(imageSource);
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (bitmapHandle != 0)
            {
                Gdi32.DeleteObject(bitmapHandle);
            }

            if (imageFactory is not null && Marshal.IsComObject(imageFactory))
            {
                Marshal.FinalReleaseComObject(imageFactory);
            }
        }
    }

    private static byte[]? ExtractEmbeddedIconPng(string path)
    {
        var largeIcons = new nint[1];
        var smallIcons = new nint[1];
        var extractedCount = Shell32.ExtractIconEx(
            path,
            0,
            largeIcons,
            smallIcons,
            1);
        if (extractedCount == 0)
        {
            return null;
        }

        var selectedIcon = largeIcons[0] != 0
            ? largeIcons[0]
            : smallIcons[0];
        try
        {
            return selectedIcon == 0
                ? null
                : EncodeIcon(selectedIcon);
        }
        finally
        {
            if (largeIcons[0] != 0)
            {
                User32.DestroyIcon(largeIcons[0]);
            }

            if (smallIcons[0] != 0 && smallIcons[0] != largeIcons[0])
            {
                User32.DestroyIcon(smallIcons[0]);
            }
        }
    }

    private static byte[] EncodeAndDestroyIcon(nint iconHandle)
    {
        try
        {
            return EncodeIcon(iconHandle);
        }
        finally
        {
            User32.DestroyIcon(iconHandle);
        }
    }

    private static byte[] EncodeIcon(nint iconHandle)
    {
        var imageSource = Imaging.CreateBitmapSourceFromHIcon(
            iconHandle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(64, 64));
        imageSource.Freeze();

        return EncodeImageSource(imageSource);
    }

    private static byte[] EncodeImageSource(BitmapSource imageSource)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(imageSource));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
