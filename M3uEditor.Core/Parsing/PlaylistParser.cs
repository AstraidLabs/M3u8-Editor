using System.Globalization;
using M3uEditor.Core.Parsing.Helpers;

namespace M3uEditor.Core.Parsing;

public class PlaylistParser : IPlaylistParser, IKindDetector
{
    public static PlaylistDocument ParseText(string text) => new PlaylistParser().Parse(text);

    public PlaylistDocument Parse(string text)
    {
        var document = new PlaylistDocument();
        var workingText = text ?? string.Empty;

        if (workingText.StartsWith('\uFEFF'))
        {
            document.HadUtf8Bom = true;
            workingText = workingText[1..];
        }

        document.NewLine = DetectNewLine(workingText);

        var rawLines = workingText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var i = 0; i < rawLines.Length; i++)
        {
            var raw = rawLines[i];
            LineNode node = CreateNode(raw, i + 1);
            document.Lines.Add(node);
        }

        document.DetectedKind = DetectKind(document.Lines);

        AnalyzeSyntax(document);
        return document;
    }

    public PlaylistKind DetectKind(IReadOnlyList<LineNode> nodes)
    {
        var hasExtX = false;
        var hasStreamInf = false;
        var hasExtInf = false;

        foreach (var line in nodes)
        {
            if (line is not TagLine tagLine)
            {
                continue;
            }

            if (tagLine.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                hasExtInf = true;
            }

            if (tagLine.TagName.StartsWith("EXT-X-", StringComparison.OrdinalIgnoreCase))
            {
                hasExtX = true;
            }

            if (tagLine.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
            {
                hasStreamInf = true;
            }
        }

        if (hasExtX)
        {
            return hasStreamInf ? PlaylistKind.HlsMaster : PlaylistKind.HlsMedia;
        }

        if (hasExtInf)
        {
            return PlaylistKind.ExtendedM3u;
        }

        return PlaylistKind.PlainM3u;
    }

    private static string DetectNewLine(string text)
    {
        var firstCarriageReturn = text.IndexOf("\r\n", StringComparison.Ordinal);
        return firstCarriageReturn >= 0 ? "\r\n" : "\n";
    }

    private static LineNode CreateNode(string raw, int lineNumber)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return new BlankLine { Raw = raw, LineNumber = lineNumber };
        }

        if (raw.StartsWith("#EXT", StringComparison.OrdinalIgnoreCase))
        {
            var tagBody = raw[1..];
            var separatorIndex = tagBody.IndexOf(':');
            var tagName = separatorIndex >= 0 ? tagBody[..separatorIndex].Trim() : tagBody.Trim();
            var tagValue = separatorIndex >= 0 ? tagBody[(separatorIndex + 1)..] : null;
            return new TagLine
            {
                Raw = raw,
                LineNumber = lineNumber,
                TagName = tagName,
                TagValue = tagValue
            };
        }

        if (raw.StartsWith("#"))
        {
            return new CommentLine { Raw = raw, LineNumber = lineNumber };
        }

        return new UriLine { Raw = raw, LineNumber = lineNumber, Value = raw };
    }

    private void AnalyzeSyntax(PlaylistDocument document)
    {
        var lines = document.Lines;
        var hasTargetDuration = false;
        var recognizedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EXTM3U",
            "EXTINF",
            "EXT-X-STREAM-INF",
            "EXT-X-VERSION",
            "EXT-X-TARGETDURATION",
            "EXT-X-MEDIA-SEQUENCE",
            "EXT-X-KEY",
            "EXT-X-MAP",
            "EXT-X-DISCONTINUITY",
            "EXT-X-PROGRAM-DATE-TIME",
            "EXT-X-BYTERANGE",
            "EXT-X-ENDLIST",
            "EXT-X-PLAYLIST-TYPE",
            "EXT-X-INDEPENDENT-SEGMENTS",
            "EXT-X-START"
        };

        for (var i = 0; i < lines.Count; i++)
        {
            switch (lines[i])
            {
                case UriLine uri:
                    if (string.IsNullOrWhiteSpace(uri.Value))
                    {
                        document.Diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Warning,
                            "URI001",
                            "URI line is empty.",
                            new TextSpan(i, 0, uri.Raw.Length)));
                    }

                    break;
                case TagLine tag:
                    if (tag.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
                    {
                        var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(lines, i);
                        if (uriIndex < 0 || lines[uriIndex] is not UriLine)
                        {
                            document.Diagnostics.Add(new Diagnostic(
                                DiagnosticSeverity.Error,
                                "TAG001",
                                "#EXTINF must be followed by a URI line.",
                                new TextSpan(i, 0, tag.Raw.Length)));
                        }

                        ValidateExtInfDuration(document, tag, i);
                    }
                    else if (tag.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
                    {
                        var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(lines, i);
                        if (uriIndex < 0 || lines[uriIndex] is not UriLine)
                        {
                            document.Diagnostics.Add(new Diagnostic(
                                DiagnosticSeverity.Error,
                                "TAG002",
                                "#EXT-X-STREAM-INF must be followed by a URI line.",
                                new TextSpan(i, 0, tag.Raw.Length)));
                        }

                        if (HasUnclosedQuotes(tag.TagValue))
                        {
                            document.Diagnostics.Add(new Diagnostic(
                                DiagnosticSeverity.Error,
                                "TAG003",
                                "Attribute list contains an unclosed quote.",
                                new TextSpan(i, 0, tag.Raw.Length)));
                        }
                    }
                    else if (tag.TagName.Equals("EXT-X-TARGETDURATION", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTargetDuration = true;
                        if (!int.TryParse(tag.TagValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var duration) || duration <= 0)
                        {
                            document.Diagnostics.Add(new Diagnostic(
                                DiagnosticSeverity.Warning,
                                "HLS001",
                                "Invalid TARGETDURATION value.",
                                new TextSpan(i, 0, tag.Raw.Length)));
                        }
                    }
                    else if (tag.TagName.Equals("EXT-X-MEDIA-SEQUENCE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!long.TryParse(tag.TagValue?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        {
                            document.Diagnostics.Add(new Diagnostic(
                                DiagnosticSeverity.Warning,
                                "HLS002",
                                "Invalid MEDIA-SEQUENCE value.",
                                new TextSpan(i, 0, tag.Raw.Length)));
                        }
                    }
                    else if (tag.TagName.StartsWith("EXT-X-", StringComparison.OrdinalIgnoreCase) && HasUnclosedQuotes(tag.TagValue))
                    {
                        document.Diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Warning,
                            "TAG003",
                            "Attribute list contains an unclosed quote.",
                            new TextSpan(i, 0, tag.Raw.Length)));
                    }

                    if (tag.TagName.StartsWith("EXT", StringComparison.OrdinalIgnoreCase) && !recognizedTags.Contains(tag.TagName))
                    {
                        document.Diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Info,
                            "TAGINF",
                            $"Unknown tag {tag.TagName} is preserved.",
                            new TextSpan(i, 0, tag.Raw.Length)));
                    }

                    break;
            }
        }

        if (document.DetectedKind == PlaylistKind.HlsMedia && !hasTargetDuration)
        {
            document.Diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Warning,
                "HLS003",
                "HLS media playlist is missing #EXT-X-TARGETDURATION.",
                new TextSpan(0, 0, lines.Count > 0 ? lines[0].Raw.Length : 0)));
        }
    }

    private void ValidateExtInfDuration(PlaylistDocument document, TagLine tag, int lineIndex)
    {
        if (string.IsNullOrEmpty(tag.TagValue))
        {
            return;
        }

        var hasDuration = ExtInfParser.TryParse(tag.TagValue, out var duration, out _, out _);

        var errorCode = document.DetectedKind == PlaylistKind.HlsMedia || document.DetectedKind == PlaylistKind.HlsMaster
            ? "HLSINF"
            : "IPTVINF";

        var severity = DiagnosticSeverity.Error;
        var span = new TextSpan(lineIndex, 0, tag.Raw.Length);

        if (document.DetectedKind == PlaylistKind.HlsMedia || document.DetectedKind == PlaylistKind.HlsMaster)
        {
            if (!hasDuration || duration is null)
            {
                document.Diagnostics.Add(new Diagnostic(severity, errorCode, "Invalid HLS EXTINF duration.", span));
            }
        }
        else if (!hasDuration)
        {
            document.Diagnostics.Add(new Diagnostic(severity, errorCode, "Invalid IPTV EXTINF duration.", span));
        }
    }

    private static bool HasUnclosedQuotes(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var quoteCount = 0;
        foreach (var ch in value)
        {
            if (ch == '"')
            {
                quoteCount++;
            }
        }

        return quoteCount % 2 != 0;
    }
}
