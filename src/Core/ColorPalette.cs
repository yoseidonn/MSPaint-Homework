using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Core
{
    /// <summary>
    /// Color palette - provides default colors and color lookup
    /// </summary>
    public static class ColorPalette
    {
        public static readonly MediaColor[] DefaultColors = new[]
        {
            MediaColors.Black,
            MediaColors.Gray,
            MediaColors.White,
            MediaColors.Red,
            MediaColors.Lime,
            MediaColors.Cyan,
            MediaColors.Blue
        };

        public static MediaColor GetColorByName(string name)
        {
            return name?.ToLower() switch
            {
                "black" => MediaColors.Black,
                "gray" => MediaColors.Gray,
                "white" => MediaColors.White,
                "red" => MediaColors.Red,
                "lime" => MediaColors.Lime,
                "cyan" => MediaColors.Cyan,
                "blue" => MediaColors.Blue,
                _ => MediaColors.Black
            };
        }
    }
}
