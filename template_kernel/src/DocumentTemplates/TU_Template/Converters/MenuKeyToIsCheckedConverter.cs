using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TU_Template.Converters;

/// <summary>
/// Compares viewmodel's ActiveMenu key with ConverterParameter and returns boolean for IsChecked.
/// </summary>
public class MenuKeyToIsCheckedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var activeMenuKey = value?.ToString();
        var thisMenuKey = parameter?.ToString();

        return !string.IsNullOrWhiteSpace(activeMenuKey)
               && !string.IsNullOrWhiteSpace(thisMenuKey)
               && string.Equals(activeMenuKey, thisMenuKey, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

