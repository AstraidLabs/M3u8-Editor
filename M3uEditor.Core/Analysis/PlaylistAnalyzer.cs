using System.Globalization;
using M3uEditor.Core.Parsing.Helpers;

namespace M3uEditor.Core.Analysis;

public static class PlaylistAnalyzer
{
    public static List<Diagnostic> Analyze(PlaylistDocument document)
    {
        var diagnostics = new List<Diagnostic>(document.Diagnostics);
        AddDuplicateUriWarnings(document, diagnostics);

        if (document.DetectedKind == PlaylistKind.HlsMedia || document.DetectedKind == PlaylistKind.HlsMaster)
        {
            AddHlsDurationWarnings(document, diagnostics);
        }

        if (document.DetectedKind == PlaylistKind.HlsMaster)
        {
            ValidateStreamInfAttributes(document, diagnostics);
        }

        return diagnostics;
    }

    private static void AddDuplicateUriWarnings(PlaylistDocument document, List<Diagnostic> diagnostics)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < document.Lines.Count; i++)
        {
            if (document.Lines[i] is not UriLine uriLine)
            {
                continue;
            }

            var normalized = uriLine.Value.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                continue;
            }

            if (seen.TryGetValue(normalized, out var previousIndex))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    "URI002",
                    $"Duplicate URI detected (previous at line {previousIndex + 1}).",
                    new TextSpan(i, 0, uriLine.Raw.Length)));
            }
            else
            {
                seen[normalized] = i;
            }
        }
    }

    private static void AddHlsDurationWarnings(PlaylistDocument document, List<Diagnostic> diagnostics)
    {
        double? targetDuration = null;
        foreach (var line in document.Lines.OfType<TagLine>())
        {
            if (line.TagName.Equals("EXT-X-TARGETDURATION", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(line.TagValue?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                targetDuration = parsed;
                break;
            }
        }

        var pendingDurations = new Dictionary<int, (double Duration, int ExtInfIndex)>();
        for (var i = 0; i < document.Lines.Count; i++)
        {
            var line = document.Lines[i];
            if (line is TagLine tag && tag.TagName.Equals("EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseDuration(tag.TagValue, out var duration))
                {
                    var uriIndex = PlaylistLineNavigator.FindNextUriLineIndex(document.Lines, i);
                    if (uriIndex >= 0)
                    {
                        pendingDurations[uriIndex] = (duration, i);
                    }
                }
            }
            else if (line is UriLine && pendingDurations.TryGetValue(i, out var segment))
            {
                if (targetDuration is not null && segment.Duration > targetDuration.Value)
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        "HLS004",
                        $"Segment duration {segment.Duration.ToString(CultureInfo.InvariantCulture)} exceeds TARGETDURATION {targetDuration.Value.ToString(CultureInfo.InvariantCulture)}.",
                        new TextSpan(segment.ExtInfIndex, 0, document.Lines[segment.ExtInfIndex].Raw.Length)));
                }
            }
        }
    }

    private static void ValidateStreamInfAttributes(PlaylistDocument document, List<Diagnostic> diagnostics)
    {
        for (var i = 0; i < document.Lines.Count; i++)
        {
            if (document.Lines[i] is not TagLine tagLine
                || !tagLine.TagName.Equals("EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var attributes = AttributeHelper.ParseAttributeList(tagLine.TagValue ?? string.Empty, ',');
            if (!attributes.Keys.Any(k => k.Equals("BANDWIDTH", StringComparison.OrdinalIgnoreCase)))
            {
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    "HLS005",
                    "Master playlist variant is missing BANDWIDTH attribute.",
                    new TextSpan(i, 0, tagLine.Raw.Length)));
            }
        }
    }

    private static bool TryParseDuration(string? extInfValue, out double duration)
    {
        duration = 0;
        if (!ExtInfParser.TryParse(extInfValue, out var parsed, out _, out _))
        {
            return false;
        }

        duration = parsed ?? 0;
        return true;
    }
}
