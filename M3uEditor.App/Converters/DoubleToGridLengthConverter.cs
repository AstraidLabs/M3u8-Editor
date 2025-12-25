using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double doubleValue && !double.IsNaN(doubleValue))
        {
            var length = Math.Max(0, doubleValue);
            return new GridLength(length);
        }

        return new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is GridLength gridLength && gridLength.IsAbsolute)
        {
            return gridLength.Value;
        }

        return DependencyProperty.UnsetValue;
    }
}
