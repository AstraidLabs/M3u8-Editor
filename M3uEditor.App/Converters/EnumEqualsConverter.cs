using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace M3uEditor.App.Converters;

public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        var valueType = value.GetType();
        if (!valueType.IsEnum)
        {
            return false;
        }

        if (Enum.TryParse(valueType, parameter.ToString(), out var parsed))
        {
            return value.Equals(parsed);
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isChecked && isChecked && targetType.IsEnum && parameter is string enumName)
        {
            return Enum.Parse(targetType, enumName);
        }

        return DependencyProperty.UnsetValue;
    }
}
