using System.Collections.ObjectModel;

namespace M3uEditor.Core;

public abstract record LineNode
{
    public string Raw { get; set; } = string.Empty;
    public int LineNumber { get; init; }
    public bool IsModified { get; set; }
}

public sealed record BlankLine : LineNode;

public sealed record CommentLine : LineNode;

public sealed record TagLine : LineNode
{
    public string TagName { get; set; } = string.Empty;
    public string? TagValue { get; set; }
}

public sealed record UriLine : LineNode
{
    public string Value { get; set; } = string.Empty;
}

public sealed class PlaylistDocument
{
    public string? OriginalPath { get; set; }

    public string NewLine { get; set; } = "\n";

    public bool HadUtf8Bom { get; set; }

    public PlaylistKind DetectedKind { get; set; }

    public List<LineNode> Lines { get; } = new();

    public List<Diagnostic> Diagnostics { get; } = new();

    public ReadOnlyCollection<LineNode> AsReadOnly() => Lines.AsReadOnly();
}
