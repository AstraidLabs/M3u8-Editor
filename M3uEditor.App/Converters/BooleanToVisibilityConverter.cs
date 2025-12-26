using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // bool? z bindingu sem stejnì pøijde jako bool nebo null
        var isVisible = value is null || (value is bool b && b);

        if (Invert)
            isVisible = !isVisible;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;
            return Invert ? !result : result;
        }

        return DependencyProperty.UnsetValue;
    }
}
