using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CustomTabControl.Controls;

/// <summary>Reusable cursors for tab interaction.</summary>
public static class TabCursors
{
    private static Cursor? closedHand;
    public static Cursor ClosedHand => closedHand ??= CreateClosedHand();

    private static Cursor CreateClosedHand()
    {
        const int size = 32;
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            // Knuckles, curled fingers and folded thumb; white fill works on both themes.
            var outline = Geometry.Parse("M 9,26 L 8,22 C 5,20 4,17 4,14 C 4,12 6,11 8,13 L 9,14 L 9,10 C 9,7 13,7 13,10 L 13,8 C 13,5 17,5 17,8 L 17,9 C 18,6 21,7 21,10 L 21,11 C 23,8 26,10 26,13 L 26,18 C 26,21 24,23 23,26 Z");
            drawing.DrawGeometry(Brushes.White, new Pen(Brushes.Black, 1.5) { LineJoin = PenLineJoin.Round }, outline);
            var detail = Geometry.Parse("M 13,10 L 13,14 M 17,9 L 17,14 M 21,11 L 21,15 M 8,14 C 10,14 11,16 12,18 L 16,18");
            drawing.DrawGeometry(null, new Pen(Brushes.Black, 1.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, detail);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[size * size * 4];
        bitmap.CopyPixels(pixels, size * 4, 0);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            // CUR directory, center-of-grip hotspot and a 32-bit DIB with transparency mask.
            writer.Write((ushort)0); writer.Write((ushort)2); writer.Write((ushort)1);
            writer.Write((byte)size); writer.Write((byte)size); writer.Write((ushort)0);
            writer.Write((ushort)15); writer.Write((ushort)16);
            writer.Write(40 + pixels.Length + size * 4); writer.Write(22);
            writer.Write(40); writer.Write(size); writer.Write(size * 2);
            writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(0); writer.Write(pixels.Length); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
            for (var y = size - 1; y >= 0; y--)
                for (var x = 0; x < size; x++)
                {
                    var offset = (y * size + x) * 4;
                    var alpha = pixels[offset + 3];
                    for (var channel = 0; channel < 3; channel++)
                        writer.Write(alpha == 0 ? (byte)0 : (byte)Math.Min(255, pixels[offset + channel] * 255 / alpha));
                    writer.Write(alpha);
                }
            for (var y = size - 1; y >= 0; y--)
                for (var group = 0; group < 4; group++)
                {
                    byte mask = 0;
                    for (var bit = 0; bit < 8; bit++)
                        if (pixels[(y * size + group * 8 + bit) * 4 + 3] == 0) mask |= (byte)(128 >> bit);
                    writer.Write(mask);
                }
        }
        stream.Position = 0;
        return new Cursor(stream, true);
    }
}
