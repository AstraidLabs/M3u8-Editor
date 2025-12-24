using System.Globalization;

namespace M3uEditor.Core.Editing;

public static class PlaylistEditor
{
    public static void UpdateUri(PlaylistDocument document, int lineIndex, string newUri)
    {
        if (document.Lines[lineIndex] is not UriLine uriLine)
        {
            return;
        }

        uriLine.Value = newUri;
        uriLine.Raw = newUri;
        uriLine.IsModified = true;
    }

    public static void UpdateIptvMetadata(PlaylistDocument document, int uriLineIndex, int duration, string? title, IDictionary<string, string>? attributes)
    {
        var extInfLine = FindExtInfForUri(document, uriLineIndex);
        if (extInfLine is null)
        {
            InsertExtInfBeforeUri(document, uriLineIndex, duration, title ?? string.Empty, attributes);
            return;
        }

        var metadata = ParseExtInf(extInfLine.TagValue, false);
        metadata.Duration = duration;
        metadata.Title = title ?? metadata.Title;
        if (attributes is not null)
        {
            foreach (var kvp in attributes)
            {
                metadata.Attributes.AddOrUpdate(kvp.Key, QuoteIfNeeded(kvp.Value));
            }
        }

        ApplyExtInf(extInfLine, metadata, false);
    }

    public static void UpdateStreamInfAttribute(PlaylistDocument document, int tagLineIndex, string attributeName, string attributeValue)
    {
        if (document.Lines[tagLineIndex] is not TagLine tagLine
            || !tagLine.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var attributes = AttributeHelper.ParseAttributeList(tagLine.TagValue ?? string.Empty, ',', includeEmpty: true);
        attributes.AddOrUpdate(attributeName, attributeValue);
        tagLine.TagValue = attributes.ToJoinedString(',', includeSpace: true);
        tagLine.Raw = $"#{tagLine.TagName}:{tagLine.TagValue}";
        tagLine.IsModified = true;
    }

    public static void UpdateHlsExtInf(PlaylistDocument document, int tagLineIndex, double duration, string title)
    {
        if (document.Lines[tagLineIndex] is not TagLine tagLine
            || !tagLine.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var metadata = ParseExtInf(tagLine.TagValue, true);
        metadata.Duration = duration;
        metadata.Title = title;
        ApplyExtInf(tagLine, metadata, true);
    }

    private static void InsertExtInfBeforeUri(PlaylistDocument document, int uriLineIndex, int duration, string title, IDictionary<string, string>? attributes)
    {
        var collection = new AttributeCollection();
        if (attributes is not null)
        {
            foreach (var pair in attributes)
            {
                collection.Add(new AttributeEntry(pair.Key, QuoteIfNeeded(pair.Value)));
            }
        }

        var metadata = new ExtInfMetadata
        {
            Duration = duration,
            Title = title,
            Attributes = collection
        };

        var tagLine = new TagLine
        {
            TagName = "EXTINF",
            TagValue = string.Empty,
            LineNumber = document.Lines[uriLineIndex].LineNumber,
            IsModified = true
        };

        ApplyExtInf(tagLine, metadata, false);
        document.Lines.Insert(uriLineIndex, tagLine);
        Reindex(document);
    }

    private static TagLine? FindExtInfForUri(PlaylistDocument document, int uriLineIndex)
    {
        if (uriLineIndex <= 0)
        {
            return null;
        }

        return document.Lines[uriLineIndex - 1] is TagLine tag && tag.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase)
            ? tag
            : null;
    }

    private static ExtInfMetadata ParseExtInf(string? value, bool hls)
    {
        var metadata = new ExtInfMetadata();
        if (string.IsNullOrEmpty(value))
        {
            return metadata;
        }

        var commaIndex = value.IndexOf(',');
        var head = commaIndex >= 0 ? value[..commaIndex] : value;
        metadata.Title = commaIndex >= 0 ? value[(commaIndex + 1)..] : string.Empty;

        string durationText = head;
        string attributeText = string.Empty;

        if (!hls)
        {
            var spaceIndex = head.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                durationText = head[..spaceIndex];
                attributeText = head[(spaceIndex + 1)..];
            }
        }
        else
        {
            durationText = head;
        }

        if (double.TryParse(durationText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            metadata.Duration = parsed;
        }

        metadata.Attributes = hls
            ? AttributeHelper.ParseAttributeList(attributeText, ',', includeEmpty: false)
            : AttributeHelper.ParseAttributeList(attributeText, ' ', includeEmpty: false);

        return metadata;
    }

    private static void ApplyExtInf(TagLine tagLine, ExtInfMetadata metadata, bool hls)
    {
        var durationText = metadata.Duration?.ToString(metadata.Duration % 1 == 0 ? "0" : "0.###", CultureInfo.InvariantCulture) ?? string.Empty;
        var attributesPart = metadata.Attributes.ToJoinedString(hls ? ',' : ' ', includeSpace: !hls);

        var head = string.IsNullOrWhiteSpace(attributesPart)
            ? durationText
            : $"{durationText}{(hls ? "," : " ")}{attributesPart}";

        tagLine.TagValue = $"{head},{metadata.Title}";
        tagLine.Raw = $"#{tagLine.TagName}:{tagLine.TagValue}";
        tagLine.IsModified = true;
    }

    private static void Reindex(PlaylistDocument document)
    {
        for (var i = 0; i < document.Lines.Count; i++)
        {
            var line = document.Lines[i];
            switch (line)
            {
                case TagLine tag:
                    document.Lines[i] = tag with { LineNumber = i + 1 };
                    break;
                case UriLine uri:
                    document.Lines[i] = uri with { LineNumber = i + 1 };
                    break;
                case CommentLine comment:
                    document.Lines[i] = comment with { LineNumber = i + 1 };
                    break;
                case BlankLine blank:
                    document.Lines[i] = blank with { LineNumber = i + 1 };
                    break;
            }
        }
    }

    private static string QuoteIfNeeded(string value)
    {
        if (value.Length > 0 && value.StartsWith("\"", StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value}\"";
    }
}

public sealed class ExtInfMetadata
{
    public double? Duration { get; set; }

    public string Title { get; set; } = string.Empty;

    public AttributeCollection Attributes { get; set; } = new();
}
