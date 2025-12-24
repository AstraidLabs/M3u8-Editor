using System.Text;

namespace M3uEditor.Core.Writing;

public static class PlaylistWriter
{
    public static string Write(PlaylistDocument document, bool? includeBom = null)
    {
        var builder = new StringBuilder();
        var useBom = includeBom ?? document.HadUtf8Bom;

        if (useBom)
        {
            builder.Append('\uFEFF');
        }

        for (var i = 0; i < document.Lines.Count; i++)
        {
            var line = document.Lines[i];
            builder.Append(BuildLineText(line));

            if (i < document.Lines.Count - 1)
            {
                builder.Append(document.NewLine);
            }
        }

        return builder.ToString();
    }

    private static string BuildLineText(LineNode line)
    {
        if (!line.IsModified && !string.IsNullOrEmpty(line.Raw))
        {
            return line.Raw;
        }

        return line switch
        {
            TagLine tag => BuildTagLine(tag),
            UriLine uri => uri.Value,
            _ => line.Raw
        };
    }

    private static string BuildTagLine(TagLine tag)
    {
        return tag.TagValue is null
            ? $"#{tag.TagName}"
            : $"#{tag.TagName}:{tag.TagValue}";
    }
}
