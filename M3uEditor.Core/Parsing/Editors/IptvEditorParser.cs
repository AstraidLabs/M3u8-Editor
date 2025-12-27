using M3uEditor.Core;
using M3uEditor.Core.Parsing.Helpers;
using M3uEditor.Core.Projection;

namespace M3uEditor.Core.Parsing.Editors;

public sealed class IptvEditorParser : IEditorParser<ProjectionResult<IptvItem>>
{
    public ProjectionResult<IptvItem> Parse(PlaylistDocument document)
    {
        var projection = new ProjectionResult<IptvItem>();
        var usedUriIndices = new HashSet<int>();
        var lines = document.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] is TagLine tagLine && tagLine.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(lines, i);
                if (uriIndex >= 0 && !usedUriIndices.Contains(uriIndex) && lines[uriIndex] is UriLine uriLine)
                {
                    ExtInfParser.TryParse(tagLine.TagValue, out _, out var title, out var attributesText);
                    var attributes = AttributeHelper.ParseAttributeList(attributesText, ' ', includeEmpty: false);

                    var item = new IptvItem
                    {
                        ExtInfLineIndex = i,
                        UriLineIndex = uriIndex,
                        Title = title,
                        Url = uriLine.Value,
                        Attributes = attributes
                    };

                    var index = projection.Items.Count;
                    projection.Items.Add(item);
                    projection.LineToItem[i] = index;
                    projection.LineToItem[uriIndex] = index;
                    usedUriIndices.Add(uriIndex);
                }
            }
            else if (lines[i] is UriLine uriLine && !usedUriIndices.Contains(i))
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
                usedUriIndices.Add(i);
            }
        }

        return projection;
    }

    object? IEditorParser.ParseUntyped(PlaylistDocument document) => Parse(document);
}
