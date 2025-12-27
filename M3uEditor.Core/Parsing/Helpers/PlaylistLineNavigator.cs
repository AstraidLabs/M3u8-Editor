namespace M3uEditor.Core.Parsing.Helpers;

public static class PlaylistLineNavigator
{
    public static int FindNextUriLineIndex(IReadOnlyList<LineNode> lines, int startIndexExclusive)
    {
        for (var i = startIndexExclusive + 1; i < lines.Count; i++)
        {
            if (lines[i] is UriLine)
            {
                return i;
            }
        }

        return -1;
    }
}
