using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;

namespace M3uEditor.App.Behaviors;

public static class TextHighlighterExtensions
{
    public static readonly DependencyProperty RangesSourceProperty =
        DependencyProperty.RegisterAttached(
            "RangesSource",
            typeof(IEnumerable<TextRange>),
            typeof(TextHighlighterExtensions),
            new PropertyMetadata(null, OnRangesSourceChanged));

    public static IEnumerable<TextRange>? GetRangesSource(TextHighlighter obj) =>
        (IEnumerable<TextRange>?)obj.GetValue(RangesSourceProperty);

    public static void SetRangesSource(TextHighlighter obj, IEnumerable<TextRange>? value) =>
        obj.SetValue(RangesSourceProperty, value);

    private static void OnRangesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextHighlighter highlighter)
        {
            return;
        }

        highlighter.Ranges.Clear();
        if (e.NewValue is IEnumerable<TextRange> ranges)
        {
            foreach (var range in ranges)
            {
                highlighter.Ranges.Add(range);
            }
        }
    }
}
