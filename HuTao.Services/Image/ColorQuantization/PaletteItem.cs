using System.Drawing;

namespace HuTao.Services.Image.ColorQuantization;

public struct PaletteItem
{
    public Color Color { get; set; }

    public int Weight { get; set; }
}