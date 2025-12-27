using System;
using M3uEditor.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace M3uEditor.App.Converters;

public sealed class DiagnosticSeverityToBrushConverter : IValueConverter
{
    public Brush? ErrorBrush { get; set; }
    public Brush? WarningBrush { get; set; }
    public Brush? InfoBrush { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => ErrorBrush ?? new SolidColorBrush(Colors.IndianRed),
                DiagnosticSeverity.Warning => WarningBrush ?? new SolidColorBrush(Colors.Goldenrod),
                _ => InfoBrush ?? new SolidColorBrush(Colors.DeepSkyBlue)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
