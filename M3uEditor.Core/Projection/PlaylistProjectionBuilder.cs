using System.Globalization;

namespace M3uEditor.Core.Projection;

public sealed class ProjectionResult<T>
{
    public List<T> Items { get; } = new();

    public Dictionary<int, int> LineToItem { get; } = new();
}

public sealed class IptvItem
{
    public required int UriLineIndex { get; init; }
    public int? ExtInfLineIndex { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public AttributeCollection Attributes { get; init; } = new();

    public string? GetAttribute(string key) => Attributes.Find(key)?.Value;
}

public sealed class HlsMasterVariant
{
    public required int StreamInfLineIndex { get; init; }
    public required int UriLineIndex { get; init; }
    public AttributeCollection Attributes { get; init; } = new();
    public string Url { get; init; } = string.Empty;
}

public sealed class HlsMediaSegment
{
    public required int ExtInfLineIndex { get; init; }
    public required int UriLineIndex { get; init; }
    public IReadOnlyList<int> LeadingTagIndices { get; init; } = Array.Empty<int>();
    public double? Duration { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Uri { get; init; } = string.Empty;
}

public sealed class HlsMediaProjection
{
    public List<int> HeaderTagIndices { get; } = new();
    public ProjectionResult<HlsMediaSegment> Segments { get; } = new();
}

public static class PlaylistProjectionBuilder
{
    public static ProjectionResult<IptvItem> BuildIptvItems(PlaylistDocument document)
    {
        var projection = new ProjectionResult<IptvItem>();
        for (var i = 0; i < document.Lines.Count; i++)
        {
            if (document.Lines[i] is TagLine tag
                && tag.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase)
                && i + 1 < document.Lines.Count
                && document.Lines[i + 1] is UriLine uri)
            {
                var metadata = ParseExtInf(tag.TagValue);
                var item = new IptvItem
                {
                    ExtInfLineIndex = i,
                    UriLineIndex = i + 1,
                    Title = metadata.Title,
                    Url = uri.Value,
                    Attributes = metadata.Attributes
                };

                var index = projection.Items.Count;
                projection.Items.Add(item);
                projection.LineToItem[i] = index;
                projection.LineToItem[i + 1] = index;
                i++; // skip URI line
            }
            else if (document.Lines[i] is UriLine uriLine)
            {
                var item = new IptvItem
                {
                    ExtInfLineIndex = null,
                    UriLineIndex = i,
                    Title = string.Empty,
                    Url = uriLine.Value,
                    Attributes = new AttributeCollection()
                };

                var index = projection.Items.Count;
                projection.Items.Add(item);
                projection.LineToItem[i] = index;
            }
        }

        return projection;
    }

    public static ProjectionResult<HlsMasterVariant> BuildHlsMasterItems(PlaylistDocument document)
    {
        var projection = new ProjectionResult<HlsMasterVariant>();

        for (var i = 0; i < document.Lines.Count; i++)
        {
            if (document.Lines[i] is TagLine tagLine &&
                tagLine.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < document.Lines.Count &&
                document.Lines[i + 1] is UriLine uriLine)
            {
                var attributes = AttributeHelper.ParseAttributeList(tagLine.TagValue ?? string.Empty, ',', includeEmpty: true);
                var variant = new HlsMasterVariant
                {
                    StreamInfLineIndex = i,
                    UriLineIndex = i + 1,
                    Url = uriLine.Value,
                    Attributes = attributes
                };

                var index = projection.Items.Count;
                projection.Items.Add(variant);
                projection.LineToItem[i] = index;
                projection.LineToItem[i + 1] = index;
                i++;
            }
        }

        return projection;
    }

    public static HlsMediaProjection BuildHlsMediaSegments(PlaylistDocument document)
    {
        var projection = new HlsMediaProjection();
        var leadingTags = new List<int>();
        var headerComplete = false;

        for (var i = 0; i < document.Lines.Count; i++)
        {
            if (document.Lines[i] is TagLine tagLine)
            {
                if (IsLeadingTag(tagLine.TagName))
                {
                    leadingTags.Add(i);
                    if (!headerComplete)
                    {
                        projection.HeaderTagIndices.Add(i);
                    }

                    continue;
                }

                if (tagLine.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < document.Lines.Count
                    && document.Lines[i + 1] is UriLine uriLine)
                {
                    var metadata = ParseExtInf(tagLine.TagValue);
                    var segment = new HlsMediaSegment
                    {
                        ExtInfLineIndex = i,
                        UriLineIndex = i + 1,
                        LeadingTagIndices = leadingTags.ToList(),
                        Duration = metadata.Duration,
                        Title = metadata.Title,
                        Uri = uriLine.Value
                    };

                    var index = projection.Segments.Items.Count;
                    projection.Segments.Items.Add(segment);
                    projection.Segments.LineToItem[i] = index;
                    projection.Segments.LineToItem[i + 1] = index;
                    foreach (var tagIndex in leadingTags)
                    {
                        projection.Segments.LineToItem[tagIndex] = index;
                    }

                    leadingTags.Clear();
                    headerComplete = true;
                    i++;
                }
            }
        }

        return projection;
    }

    private static bool IsLeadingTag(string tagName)
    {
        return tagName.Equals("EXT-X-KEY", StringComparison.OrdinalIgnoreCase)
               || tagName.Equals("EXT-X-BYTERANGE", StringComparison.OrdinalIgnoreCase)
               || tagName.Equals("EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase)
               || tagName.Equals("EXT-X-PROGRAM-DATE-TIME", StringComparison.OrdinalIgnoreCase)
               || tagName.Equals("EXT-X-MAP", StringComparison.OrdinalIgnoreCase);
    }

    private static ExtInfInfo ParseExtInf(string? tagValue)
    {
        var info = new ExtInfInfo();
        if (string.IsNullOrEmpty(tagValue))
        {
            return info;
        }

        var commaIndex = tagValue.IndexOf(',');
        var head = commaIndex >= 0 ? tagValue[..commaIndex] : tagValue;
        info.Title = commaIndex >= 0 ? tagValue[(commaIndex + 1)..] : string.Empty;

        var spaceIndex = head.IndexOf(' ');
        var durationText = spaceIndex >= 0 ? head[..spaceIndex] : head;
        var attributeText = spaceIndex >= 0 ? head[(spaceIndex + 1)..] : string.Empty;

        if (double.TryParse(durationText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            info.Duration = parsed;
        }

        info.Attributes = AttributeHelper.ParseAttributeList(attributeText, ' ', includeEmpty: false);
        return info;
    }

    private sealed class ExtInfInfo
    {
        public double? Duration { get; set; }

        public string Title { get; set; } = string.Empty;

        public AttributeCollection Attributes { get; set; } = new();
    }
}
