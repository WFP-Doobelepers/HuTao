using System.Drawing;
using System.IO;
using ColorThiefDotNet;
using DColor = Discord.Color;
using SColor = System.Drawing.Color;

namespace Zhongli.Services.Utilities
{
    public static class ImageExtensions
    {
        public static Bitmap ToBitmap(this byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            return (Bitmap) System.Drawing.Image.FromStream(stream);
        }

        public static DColor ToDiscordColor(this QuantizedColor color)
        {
            var c = color.Color;
            return (DColor) SColor.FromArgb(c.A, c.R, c.G, c.B);
        }
    }
}