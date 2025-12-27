using M3uEditor.Core;
using M3uEditor.Core.Parsing.Helpers;
using M3uEditor.Core.Projection;

namespace M3uEditor.Core.Parsing.Editors;

public sealed class HlsMediaEditorParser : IEditorParser<HlsMediaProjection>
{
    public HlsMediaProjection Parse(PlaylistDocument document)
    {
        var projection = new HlsMediaProjection();
        var leadingTags = new List<int>();
        var usedUriIndices = new HashSet<int>();
        var headerComplete = false;
        var lines = document.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] is TagLine tagLine)
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

                if (!tagLine.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(lines, i);
                if (uriIndex < 0 || usedUriIndices.Contains(uriIndex) || lines[uriIndex] is not UriLine uriLine)
                {
                    continue;
                }

                var leadingForSegment = new List<int>(leadingTags);
                for (var j = i + 1; j < uriIndex; j++)
                {
                    if (lines[j] is TagLine intermediateTag && IsLeadingTag(intermediateTag.TagName))
                    {
                        leadingForSegment.Add(j);
                    }
                }

                ExtInfParser.TryParse(tagLine.TagValue, out var duration, out var title, out _);
                var segment = new HlsMediaSegment
                {
                    ExtInfLineIndex = i,
                    UriLineIndex = uriIndex,
                    LeadingTagIndices = leadingForSegment,
                    Duration = duration,
                    Title = title,
                    Uri = uriLine.Value
                };

                var index = projection.Segments.Items.Count;
                projection.Segments.Items.Add(segment);
                projection.Segments.LineToItem[i] = index;
                projection.Segments.LineToItem[uriIndex] = index;
                foreach (var tagIndex in leadingForSegment)
                {
                    projection.Segments.LineToItem[tagIndex] = index;
                }

                leadingTags.Clear();
                headerComplete = true;
                usedUriIndices.Add(uriIndex);
                i = uriIndex;
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

    object? IEditorParser.ParseUntyped(PlaylistDocument document) => Parse(document);
}
