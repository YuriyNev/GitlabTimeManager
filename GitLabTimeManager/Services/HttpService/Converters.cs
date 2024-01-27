using System.Drawing;
using System.Globalization;

namespace GitLabTimeManager.Services;

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
}