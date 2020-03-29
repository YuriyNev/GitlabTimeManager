using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace GitLabTimeManager.Helpers
{
    public static class ColorHelper
    {
        public static ObservableCollection<SolidColorBrush> Colors { get; }
        private static Dictionary<uint, SolidColorBrush> ColorsDictionary { get; }

        public static uint DefaultColor => ColorToUInt(Colors[0].Color);

        static ColorHelper()
        {
            Colors = new ObservableCollection<SolidColorBrush>();
            ColorsDictionary = new Dictionary<uint, SolidColorBrush>();

            if (Application.Current.TryFindResource("AccentBaseColor") is Color defaultColor)
                AddFirstColors(defaultColor);
        }

        private static void AddFirstColors(Color defaultColor)
        {
            GetOrAddBrush(ColorToUInt(defaultColor));
            GetOrAddBrush(0xFFFFFFFF); // White
            GetOrAddBrush(0xFF000000); // Black
            GetOrAddBrush(0xFFA20025); // Crimson
            GetOrAddBrush(0xFFE51400); // Red
            GetOrAddBrush(0xFFFA6800); // Orange
            GetOrAddBrush(0xFFF0A30A); // Amber
            GetOrAddBrush(0xFFFEDE06); // Yellow
            GetOrAddBrush(0xFFA4C400); // Lime
            GetOrAddBrush(0xFF60A917); // Green
            GetOrAddBrush(0xFF008A00); // Emerald
            GetOrAddBrush(0xFF00ABA9); // Teal
            GetOrAddBrush(0xFF119EDA); // Blue
            GetOrAddBrush(0xFF0050EF); // Cobalt
            GetOrAddBrush(0xFF6459DF); // Purple
            GetOrAddBrush(0xFF6A00FF); // Indigo
            GetOrAddBrush(0xFFAA00FF); // Violet
            GetOrAddBrush(0xFFF472D0); // Pink
            GetOrAddBrush(0xFF76608A); // Mauve
            GetOrAddBrush(0xFF647687); // Steel
            GetOrAddBrush(0xFF825A2C); // Brown
            GetOrAddBrush(0xFFA0522D); // Sienna
            GetOrAddBrush(0xFFA0526f); // my1
            GetOrAddBrush(0xFFA0826f); // my2
            GetOrAddBrush(0xFFA0B26f); // my3
        }

        public static Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color);
            return Color.FromArgb(a, r, g, b);
        }

        public static SolidColorBrush SolidColorBrush(uint color)
        {
            var brush = new SolidColorBrush(UIntToColor(color));
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush ColorToBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush GetOrAddBrush(Color color)
        {
            return GetOrAddBrush(ColorToUInt(color));
        }

        public static SolidColorBrush GetOrAddBrush(uint color)
        {
            if (color == 0u)
                color = DefaultColor;

            if (!ColorsDictionary.TryGetValue(color, out var brush))
            {
                brush = SolidColorBrush(color);
                Colors.Add(brush);
                ColorsDictionary.Add(color, brush);
            }

            return brush;
        }

        public static uint ColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B));
        }

        public static uint ConvertToInt(SolidColorBrush brush)
        {
            return ColorToUInt(brush.Color);
        }

        public static double GetBrightness(Color color)
        {
            return (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / byte.MaxValue;
        }

        public static SolidColorBrush GetIdealBrush(double brightness)
        {
            return brightness > 0.5 ? Brushes.Black : Brushes.White;
        }
    }
}
