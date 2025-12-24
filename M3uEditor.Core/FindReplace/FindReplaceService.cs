using System.Text.RegularExpressions;

namespace M3uEditor.Core.FindReplace;

public sealed class FindReplaceOptions
{
    public required string FindText { get; init; }

    public string ReplaceText { get; init; } = string.Empty;

    public bool MatchCase { get; init; }

    public bool WholeWord { get; init; }

    public bool UseRegex { get; init; }
}

public sealed record FindMatch(int LineIndex, int Start, int Length, string Preview);

public static class FindReplaceService
{
    public static IReadOnlyList<FindMatch> FindAll(PlaylistDocument document, FindReplaceOptions options)
    {
        var matches = new List<FindMatch>();
        for (var i = 0; i < document.Lines.Count; i++)
        {
            var raw = document.Lines[i].Raw;
            foreach (var match in FindMatchesInLine(raw, i, options))
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    public static FindMatch? FindNext(PlaylistDocument document, FindReplaceOptions options, FindMatch? current)
    {
        var all = FindAll(document, options);
        if (all.Count == 0)
        {
            return null;
        }

        if (current is null)
        {
            return all[0];
        }

        var next = all.FirstOrDefault(m =>
            m.LineIndex > current.LineIndex ||
            (m.LineIndex == current.LineIndex && m.Start > current.Start));

        return next ?? all[0];
    }

    public static FindMatch? ReplaceCurrent(PlaylistDocument document, FindMatch match, FindReplaceOptions options)
    {
        var line = document.Lines[match.LineIndex];
        var updated = ReplaceRange(line.Raw, match.Start, match.Length, options.ReplaceText);
        ApplyReplacement(line, updated);

        return FindNext(document, options, match);
    }

    public static int ReplaceAll(PlaylistDocument document, FindReplaceOptions options)
    {
        var replacements = 0;
        for (var i = 0; i < document.Lines.Count; i++)
        {
            var line = document.Lines[i];
            var raw = line.Raw;
            if (options.UseRegex)
            {
                var regexOptions = options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                var regex = new Regex(options.FindText, regexOptions);
                var newRaw = regex.Replace(raw, options.ReplaceText);
                var count = regex.Matches(raw).Count;
                if (count > 0)
                {
                    replacements += count;
                    ApplyReplacement(line, newRaw);
                }
            }
            else
            {
                var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var index = 0;
                var builder = new System.Text.StringBuilder();
                var localCount = 0;
                while (true)
                {
                    var found = raw.IndexOf(options.FindText, index, comparison);
                    if (found < 0)
                    {
                        builder.Append(raw[index..]);
                        break;
                    }

                    if (options.WholeWord && !IsWholeWordMatch(raw, found, options.FindText.Length))
                    {
                        builder.Append(raw[index..(found + options.FindText.Length)]);
                        index = found + options.FindText.Length;
                        continue;
                    }

                    builder.Append(raw[index..found]);
                    builder.Append(options.ReplaceText);
                    index = found + options.FindText.Length;
                    localCount++;
                }

                if (localCount > 0)
                {
                    replacements += localCount;
                    ApplyReplacement(line, builder.ToString());
                }
            }
        }

        return replacements;
    }

    private static IEnumerable<FindMatch> FindMatchesInLine(string raw, int lineIndex, FindReplaceOptions options)
    {
        if (options.UseRegex)
        {
            var regexOptions = options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            foreach (Match match in Regex.Matches(raw, options.FindText, regexOptions))
            {
                if (match.Success)
                {
                    yield return new FindMatch(lineIndex, match.Index, match.Length, raw);
                }
            }

            yield break;
        }

        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var index = 0;
        while (index < raw.Length)
        {
            var found = raw.IndexOf(options.FindText, index, comparison);
            if (found < 0)
            {
                break;
            }

            if (!options.WholeWord || IsWholeWordMatch(raw, found, options.FindText.Length))
            {
                yield return new FindMatch(lineIndex, found, options.FindText.Length, raw);
            }

            index = found + Math.Max(1, options.FindText.Length);
        }
    }

    private static bool IsWholeWordMatch(string raw, int start, int length)
    {
        bool IsBoundary(int idx)
        {
            if (idx < 0 || idx >= raw.Length)
            {
                return true;
            }

            return !char.IsLetterOrDigit(raw[idx]) && raw[idx] != '_';
        }

        return IsBoundary(start - 1) && IsBoundary(start + length);
    }

    private static void ApplyReplacement(LineNode line, string newRaw)
    {
        line.Raw = newRaw;
        line.IsModified = true;

        switch (line)
        {
            case UriLine uriLine:
                uriLine.Value = newRaw;
                break;
            case TagLine tagLine:
                var body = newRaw.TrimStart('#');
                var separator = body.IndexOf(':');
                tagLine.TagName = separator >= 0 ? body[..separator] : body;
                tagLine.TagValue = separator >= 0 ? body[(separator + 1)..] : null;
                break;
        }
    }

    private static string ReplaceRange(string source, int start, int length, string replacement)
    {
        return string.Concat(source.AsSpan(0, start), replacement, source.AsSpan(start + length));
    }
}
