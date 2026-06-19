using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Synapse.GUI.Converters;

public class ProbabilityToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{Math.Round(d * 100, 0)} %";
        return "—";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // nicht implementiert, Slider bindet direkt an double -> ConvertBack nicht gebraucht
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
