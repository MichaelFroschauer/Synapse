using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Synapse.GUI.Converters;

public sealed class BoxedBoolConverter : IValueConverter
{
    public static readonly BoxedBoolConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? b : value is IConvertible c ? System.Convert.ToBoolean(c, culture) : false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? b : false;
}

public sealed class BoxedDoubleConverter : IValueConverter
{
    public static readonly BoxedDoubleConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return 0d;
        if (value is double d) return d;
        if (value is float f) return (double)f;
        if (value is decimal m) return (double)m;
        if (value is IConvertible c) return System.Convert.ToDouble(c, culture);
        return 0d;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToDouble(c, culture) : 0d;
}

public sealed class BoxedIntConverter : IValueConverter
{
    public static readonly BoxedIntConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToInt32(c, culture) : 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToInt32(c, culture) : 0;
}

public sealed class BoxedLongConverter : IValueConverter
{
    public static readonly BoxedLongConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToInt64(c, culture) : 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToInt64(c, culture) : 0;
}

public sealed class BoxedDecimalConverter : IValueConverter
{
    public static readonly BoxedDecimalConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return 0m;
        if (value is decimal m) return m;
        if (value is double d) return (decimal)d;
        if (value is float f) return (decimal)f;
        if (value is IConvertible c) return System.Convert.ToDecimal(c, culture);
        return 0m;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IConvertible c ? System.Convert.ToDecimal(c, culture) : 0m;
}

public sealed class BoxedStringConverter : IValueConverter
{
    public static readonly BoxedStringConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() ?? string.Empty;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() ?? string.Empty;
}
