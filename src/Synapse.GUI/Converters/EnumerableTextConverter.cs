using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Synapse.GUI.Converters;

public sealed class EnumerableTextConverter : IValueConverter
{
    private readonly Type _declaredType;   // z.B. IEnumerable<string>, string[], IList<int>, …
    private readonly Type _elementType;
    private readonly bool _returnArray;

    public EnumerableTextConverter(Type declaredType)
    {
        _declaredType = declaredType ?? throw new ArgumentNullException(nameof(declaredType));
        (_elementType, _returnArray) = GetElementTypeAndArrayFlag(declaredType)
            ?? throw new ArgumentException($"Type {declaredType} is not a generic collection");
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        if (value is IEnumerable e && value is not string)
        {
            var parts = new List<string>();
            foreach (var item in e)
            {
                if (item is null) continue;
                parts.Add(ItemToString(item, culture));
            }
            // one entry per line
            return string.Join(Environment.NewLine, parts);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = (value?.ToString() ?? string.Empty)
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var listType = typeof(List<>).MakeGenericType(_elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var s in text)
            list.Add(ParseItem(s, _elementType, culture));

        if (_returnArray)
        {
            var arr = Array.CreateInstance(_elementType, list.Count);
            list.CopyTo(arr, 0);
            return arr;
        }

        // If the declared type is a concrete List<T>, try to create it
        if (!_declaredType.IsInterface && !_declaredType.IsAbstract)
        {
            try
            {
                // ctor with IEnumerable<T>
                var ct = _declaredType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(_elementType) });
                if (ct != null) return ct.Invoke(new object[] { list });

                // no parameter + Add
                var concrete = (IList)Activator.CreateInstance(_declaredType)!;
                foreach (var it in list) concrete.Add(it);
                return concrete;
            }
            catch
            {
                // Fallback to List<T>
            }
        }

        return list; // List<T> fulfills IEnumerable<T>/IList<T>
    }

    private static (Type elementType, bool returnArray)? GetElementTypeAndArrayFlag(Type t)
    {
        if (t.IsArray) return (t.GetElementType()!, true);

        // Search IEnumerable<T>
        var ienum = t.IsInterface && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? t
            : t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (ienum != null) return (ienum.GetGenericArguments()[0], false);
        return null;
    }

    private static string ItemToString(object item, CultureInfo culture)
    {
        var type = item.GetType();
        if (type.IsEnum) return item.ToString()!;
        if (type == typeof(double))
            return ((double)item).ToString("G", CultureInfo.InvariantCulture);
        if (type == typeof(decimal))
            return ((decimal)item).ToString("G", CultureInfo.InvariantCulture);
        if (type == typeof(float))
            return ((float)item).ToString("G", CultureInfo.InvariantCulture);
        return System.Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static object ParseItem(string s, Type elementType, CultureInfo culture)
    {
        if (elementType == typeof(string)) return s;
        if (elementType.IsEnum) return Enum.Parse(elementType, s, ignoreCase: true);

        if (typeof(IConvertible).IsAssignableFrom(elementType))
            return System.Convert.ChangeType(s, elementType, CultureInfo.InvariantCulture)!;

        // Fallback: Try Parse via TypeConverter
        var tc = System.ComponentModel.TypeDescriptor.GetConverter(elementType);
        if (tc.CanConvertFrom(typeof(string))) return tc.ConvertFrom(null, CultureInfo.InvariantCulture, s)!;

        throw new FormatException($"Cannot convert '{s}' to {elementType.Name}.");
    }
}
