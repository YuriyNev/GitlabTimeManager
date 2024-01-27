using System.Windows;
using System.Windows.Data;
using LambdaConverters;
using IValueConverter = System.Windows.Data.IValueConverter;

namespace GitLabTimeManager.Converters;

public static class Converter
{
    public static readonly IValueConverter VisibleIfTrue =
        ValueConverter.Create<bool, Visibility>(e => e.Value ? Visibility.Visible : Visibility.Collapsed);

    public static readonly IValueConverter VisibleIfZero =
        ValueConverter.Create<int, Visibility>(e => e.Value != 0 ? Visibility.Collapsed : Visibility.Visible);

    public static readonly IValueConverter TrueIfMoreDouble =
        ValueConverter.Create<double, bool, string>(e => e.Value > double.Parse(e.Parameter));

    public static readonly IValueConverter VisibleIfNotEmpty =
        ValueConverter.Create<string, Visibility>(e => string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible);

    public static readonly IValueConverter VisibleIfFalse =
        ValueConverter.Create<bool, Visibility>(e => e.Value ? Visibility.Collapsed : Visibility.Visible);

    public static readonly IValueConverter Inverse =
        ValueConverter.Create<bool, bool>(e => !e.Value);

    public static readonly IValueConverter VisibleIfNull =
        ValueConverter.Create<object, Visibility>(e => e.Value == null ? Visibility.Visible : Visibility.Collapsed);

    public static readonly IValueConverter VisibleIfNotNull =
        ValueConverter.Create<object, Visibility>(e => e.Value != null ? Visibility.Visible : Visibility.Collapsed);
}

public static class MultiConverter
{
    public static readonly IMultiValueConverter TrueIfMoreDouble =
        MultiValueConverter.Create<double, bool>(CompareDouble);

    private static bool CompareDouble(MultiValueConverterArgs<double> args)
    {
        if (args.Values.Count < 2)
            return false;
        var x = args.Values[0];
        var y = args.Values[1];
        return x > y;
    }
}