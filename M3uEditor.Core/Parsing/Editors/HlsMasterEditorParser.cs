using M3uEditor.Core;
using M3uEditor.Core.Parsing.Helpers;
using M3uEditor.Core.Projection;

namespace M3uEditor.Core.Parsing.Editors;

public sealed class HlsMasterEditorParser : IEditorParser<ProjectionResult<HlsMasterVariant>>
{
    public ProjectionResult<HlsMasterVariant> Parse(PlaylistDocument document)
    {
        var projection = new ProjectionResult<HlsMasterVariant>();
        var usedUriIndices = new HashSet<int>();
        var lines = document.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] is not TagLine tagLine || !tagLine.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(lines, i);
            if (uriIndex < 0 || usedUriIndices.Contains(uriIndex) || lines[uriIndex] is not UriLine uriLine)
            {
                continue;
            }

            var attributes = AttributeHelper.ParseAttributeList(tagLine.TagValue ?? string.Empty, ',', includeEmpty: true);
            var variant = new HlsMasterVariant
            {
                StreamInfLineIndex = i,
                UriLineIndex = uriIndex,
                Url = uriLine.Value,
                Attributes = attributes
            };

            var index = projection.Items.Count;
            projection.Items.Add(variant);
            projection.LineToItem[i] = index;
            projection.LineToItem[uriIndex] = index;
            usedUriIndices.Add(uriIndex);
        }

        return projection;
    }

    object? IEditorParser.ParseUntyped(PlaylistDocument document) => Parse(document);
}
