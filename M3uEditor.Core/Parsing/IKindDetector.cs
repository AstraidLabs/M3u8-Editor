namespace M3uEditor.Core.Parsing;

public interface IKindDetector
{
    PlaylistKind DetectKind(IReadOnlyList<LineNode> nodes);
}
