using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class CountToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = value switch
        {
            int intValue => intValue,
            null => 0,
            _ => 0
        };

        var hasItems = count > 0;
        if (Invert)
        {
            hasItems = !hasItems;
        }

        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
