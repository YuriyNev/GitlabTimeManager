using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Catel.MVVM.Converters;
using LambdaConverters;
using IValueConverter = System.Windows.Data.IValueConverter;
using System.Windows.Media;
using GitLabTimeManager.Helpers;


namespace GitLabTimeManager.Converters
{

    public static class Converter
    {
        public static readonly IValueConverter TextIfNotZero =
            ValueConverter.Create<int, string>(e => e.Value != 0 ? e.Value.ToString(e.Culture) : string.Empty);

        public static readonly IValueConverter VisibleIfTrue =
            ValueConverter.Create<bool, Visibility>(e => e.Value ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter VisibleIfNoZero =
            ValueConverter.Create<int, Visibility>(e => e.Value != 0 ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter VisibleIfZero =
            ValueConverter.Create<int, Visibility>(e => e.Value != 0 ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter TrueIfMore =
            ValueConverter.Create<int, bool, string>(e => e.Value > int.Parse(e.Parameter));

        public static readonly IValueConverter TrueIfMoreDouble =
            ValueConverter.Create<double, bool, string>(e => e.Value > double.Parse(e.Parameter));

        public static readonly IValueConverter TrueIfEmpty =
            ValueConverter.Create<string, bool>(e => string.IsNullOrEmpty(e.Value));

        public static readonly IValueConverter VisibleIfNotEmpty =
            ValueConverter.Create<string, Visibility>(e => string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter VisibleIfEmpty =
            ValueConverter.Create<string, Visibility>(e => !string.IsNullOrEmpty(e.Value) ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter TrueIfEmptyOrWhiteSpace = ValueConverter.Create<string, bool>(e => string.IsNullOrWhiteSpace(e.Value));

        public static readonly IValueConverter VisibleIfFalse =
            ValueConverter.Create<bool, Visibility>(e => e.Value ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter Inverse =
            ValueConverter.Create<bool, bool>(e => !e.Value);

        public static readonly IValueConverter InverseDouble =
            ValueConverter.Create<double, double>(e => 1 - e.Value);

        public static readonly IValueConverter TrueIfNotNull =
            ValueConverter.Create<object, bool>(e => e.Value != null);

        public static readonly IValueConverter VisibleIfNull =
            ValueConverter.Create<object, Visibility>(e => e.Value == null ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter VisibleIfNotNull =
            ValueConverter.Create<object, Visibility>(e => e.Value != null ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter GridLength =
            ValueConverter.Create<double, GridLength>(
                e => new GridLength(e.Value),
                e => e.Value.Value);

        public static readonly IValueConverter IdealForegroundBrush =
            ValueConverter.Create<Brush, Brush>(x =>
            {
                if (x.Value is SolidColorBrush solid)
                {
                    double brightness = ColorHelper.GetBrightness(solid.Color);
                    return ColorHelper.GetIdealBrush(brightness);
                }

                return Brushes.Magenta;
            }, errorStrategy: ConverterErrorStrategy.UseFallbackOrDefaultValue);

        public static readonly IValueConverter UColorToBrush =
            ValueConverter.Create<uint, SolidColorBrush>(e => ColorHelper.SolidColorBrush(e.Value));
        
        public static readonly IValueConverter ColorToBrush =
            ValueConverter.Create<Color, SolidColorBrush>(e => ColorHelper.ColorToBrush(e.Value));

        public static readonly IValueConverter UintToColor =
            ValueConverter.Create<uint, Color>(e => ColorHelper.UIntToColor(e.Value));

        public static readonly IValueConverter IsNotNull =
            ValueConverter.Create<object, bool>(e => e.Value != null);

        public static readonly IValueConverter ToUpperCase =
            ValueConverter.Create<string, string>(e => e.Value.ToUpper());

        public static readonly IValueConverter ReadableSize =
            ValueConverter.Create<Size, string>(e => $"{e.Value.Width}x{e.Value.Height}");

        public static readonly IValueConverter NullableIntToString =
            ValueConverter.Create<int?, string>(i => i.Value?.ToString(i.Culture) ?? string.Empty, ConvertBackFunction);

        public static readonly IValueConverter StringsToLine =
            ValueConverter.Create<IReadOnlyCollection<string>, string>(i => i.Value.Aggregate(new StringBuilder(), (builder, s) => builder.Append(s).Append(", ")).ToString().TrimEnd(' ', ','));


        private static int? ConvertBackFunction(ValueConverterArgs<string> s)
        {
            return string.IsNullOrEmpty(s.Value) || !int.TryParse(s.Value, NumberStyles.Any, s.Culture, out int i)
                ? (int?)null
                : i;
        }
    }

    public static class MultiConverter
    {
        public static readonly IMultiValueConverter Calculate =
            MultiValueConverter.Create<int, int, char>(CalculateValues); 
        
        public static readonly IMultiValueConverter TrueIfMoreDouble =
            MultiValueConverter.Create<double, bool>(CompareDouble);

        public static readonly IMultiValueConverter VisibleIfAllTrue =
            MultiValueConverter.Create<bool, Visibility>(e => e.Values.All(x => x) ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IMultiValueConverter AnyIsTrue =
            MultiValueConverter.Create<bool, bool>(e => e.Values.Any(x => x));

        public static readonly IMultiValueConverter AllIsTrue =
            MultiValueConverter.Create<bool, bool>(e => e.Values.All(x => x));

        public static readonly IMultiValueConverter AnyIsFalse =
            MultiValueConverter.Create<bool, bool>(e => e.Values.Any(x => !x));

        public static readonly IMultiValueConverter AllIsFalse =
            MultiValueConverter.Create<bool, bool>(e => e.Values.All(x => !x));

        public static readonly IMultiValueConverter TrueIfTheSame =
            MultiValueConverter.Create<object, bool>(e =>
            {
                for (int i = 1; i < e.Values.Count; i++)
                {
                    if (e.Values[i] != e.Values[i - 1])
                        return false;
                }

                return true;
            });

        public static readonly IMultiValueConverter TrueIfContains =
            MultiValueConverter.Create<object, bool>(e =>
            {
                // e.Values[0] is an object we search for, e.Values[1] is a collection, where we search.
                if (e.Values[1] is ICollection sentCollection)
                {
                    foreach (object collectionItemAsObject in sentCollection)
                    {
                        if (e.Values[0].GetHashCode() == collectionItemAsObject.GetHashCode()) return true;
                    }
                }

                return false;
            });

        private static int CalculateValues(MultiValueConverterArgs<int, char> args)
        {
            int sign = args.Parameter;
            int x = args.Values[0];
            int y = args.Values[1];
            return sign switch
            {
                '+' => x + y,
                '-' => x - y,
                '*' => x * y,
                '/' => x / y,
                _ => x
            };
        }

        private static bool CompareDouble(MultiValueConverterArgs<double> args)
        {
            if (args.Values.Count < 2)
                return false;
            var x = args.Values[0];
            var y = args.Values[1];
            return x > y;
        }


    }

    //public class TrueIfMoreMulti: IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (values.Length < 2)
    //            return false;

    //        if (values[0] is double x && values[1] is double y)
    //        {
    //            return x > y;
    //        }

    //        return false;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    internal static class Selector
    {
        public static DataTemplateSelector AlternatingText =
            TemplateSelector.Create<int>(
                e => e.Item % 2 == 0
                    ? (DataTemplate)((FrameworkElement)e.Container)?.FindResource("BlackWhite")
                    : (DataTemplate)((FrameworkElement)e.Container)?.FindResource("WhiteBlack"));
    }

    public static class Rule
    {
        public static ValidationRule IsNumericString =
            Validator.Create<string>(
                e => e.Value.All(char.IsDigit)
                    ? ValidationResult.ValidResult
                    : new ValidationResult(false, "Text has non-digit characters!"));
    }

    public class TimeSpanToSecondsConverter : ValueConverterBase<TimeSpan>
    {
        protected override object Convert(TimeSpan value, Type targetType, object parameter)
        {
            return (int)value.TotalSeconds;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter)
        {
            try
            {
                return TimeSpan.FromSeconds(System.Convert.ToDouble(value));
            }
            catch (Exception)
            {
                return base.ConvertBack(value, targetType, parameter);
            }
        }
    }
}