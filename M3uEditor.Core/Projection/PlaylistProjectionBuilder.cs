using M3uEditor.Core;
using M3uEditor.Core.Parsing.Editors;

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

internal static class PlaylistProjectionBuilder
{
    internal static ProjectionResult<IptvItem> BuildIptvItems(PlaylistDocument document)
    {
        return new IptvEditorParser().Parse(document);
    }

    internal static ProjectionResult<HlsMasterVariant> BuildHlsMasterItems(PlaylistDocument document)
    {
        return new HlsMasterEditorParser().Parse(document);
    }

    internal static HlsMediaProjection BuildHlsMediaSegments(PlaylistDocument document)
    {
        return new HlsMediaEditorParser().Parse(document);
    }
}
