using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isVisible;

        if (value is bool boolValue)
        {
            isVisible = boolValue;
        }
        else if (value is bool? nullableBool)
        {
            isVisible = nullableBool ?? false;
        }
        else
        {
            isVisible = false;
        }

        if (Invert)
        {
            isVisible = !isVisible;
        }

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
