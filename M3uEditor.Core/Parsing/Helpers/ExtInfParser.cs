using System.Globalization;

namespace M3uEditor.Core.Parsing.Helpers;

public static class ExtInfParser
{
    public static bool TryParse(string? tagValue, out double? duration, out string title, out string attributesText)
    {
        duration = null;
        title = string.Empty;
        attributesText = string.Empty;

        if (string.IsNullOrWhiteSpace(tagValue))
        {
            return false;
        }

        var commaIndex = tagValue.IndexOf(',');
        var head = commaIndex >= 0 ? tagValue[..commaIndex] : tagValue;
        title = commaIndex >= 0 ? tagValue[(commaIndex + 1)..] : string.Empty;

        var trimmedHead = head.TrimStart();
        var spaceIndex = trimmedHead.IndexOf(' ');
        var durationText = spaceIndex >= 0 ? trimmedHead[..spaceIndex] : trimmedHead;
        attributesText = spaceIndex >= 0 ? trimmedHead[(spaceIndex + 1)..] : string.Empty;

        if (!double.TryParse(durationText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        duration = parsed;
        return true;
    }
}
