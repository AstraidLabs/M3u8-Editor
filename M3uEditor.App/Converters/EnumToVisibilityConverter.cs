using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || parameter is null)
        {
            return Visibility.Collapsed;
        }

        var valueType = value.GetType();
        if (!valueType.IsEnum)
        {
            return Visibility.Collapsed;
        }

        if (Enum.TryParse(valueType, parameter.ToString(), out var parsed))
        {
            return value.Equals(parsed) ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
