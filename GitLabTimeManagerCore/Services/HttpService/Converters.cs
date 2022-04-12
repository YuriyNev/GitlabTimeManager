using System.Drawing;
using System.Globalization;

namespace GitLabTimeManager.Services
{
    internal static class Converters
    {
        private const char Separator = ',';

        public static PointF ConvertToPointF(string serializedPointF)
        {
            var values = serializedPointF.Split(Separator);

            if (values.Length > 1 &&
                float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
                return new PointF(x, y);

            return new PointF();
        }

        public static SizeF ConvertToSizeF(string serializedSizeF)
        {
            var values = serializedSizeF.Split(Separator);

            if (values.Length > 1 &&
                float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
                return new SizeF(x, y);

            return new SizeF();
        }

        public static Color ConvertToColor(string serializedColor)
        {
            var values = serializedColor.Split(Separator);

            if (values.Length == 3 &&
                byte.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var r) &&
                byte.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var g) &&
                byte.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var b))
                return Color.FromArgb(r, g, b);

            return Color.Empty;
        }
    }
}