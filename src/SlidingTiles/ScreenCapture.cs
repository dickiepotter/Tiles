using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SlidingTiles;

/// <summary>Renders a window's client area to a PNG — used to produce the README screenshot headlessly.</summary>
internal static class ScreenCapture
{
    /// <summary>Returns the path following a <c>--capture</c> argument, or <see langword="null"/>.</summary>
    public static string? PathFrom(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "--capture")
                return args[i + 1];
        return null;
    }

    /// <summary>Saves the window's rendered content (no OS chrome) as a PNG at <paramref name="path"/>.</summary>
    public static void SavePng(Window window, string path)
    {
        window.UpdateLayout();

        // Size to the client area: the content's own size plus any margin it sits inside, so a
        // margined root isn't clipped or shifted when we render the whole window.
        var root = (FrameworkElement)window.Content;
        var margin = root.Margin;
        var width = (int)Math.Ceiling(root.ActualWidth + margin.Left + margin.Right);
        var height = (int)Math.Ceiling(root.ActualHeight + margin.Top + margin.Bottom);

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);   // render the window so its Background is included

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
