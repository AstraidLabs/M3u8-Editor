namespace M3uEditor.Core.Parsing;

public interface IPlaylistParser
{
    PlaylistDocument Parse(string text);
}
