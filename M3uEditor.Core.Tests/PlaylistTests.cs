using FluentAssertions;
using M3uEditor.Core.Analysis;
using M3uEditor.Core.FindReplace;
using M3uEditor.Core.Parsing;
using M3uEditor.Core.Writing;

namespace M3uEditor.Core.Tests;

public class PlaylistTests
{
    [Fact]
    public void RoundtripPlainPlaylistPreservesContent()
    {
        var content = "track1.mp3\ntrack2.mp3";
        var document = PlaylistParser.Parse(content);

        document.DetectedKind.Should().Be(PlaylistKind.PlainM3u);
        PlaylistWriter.Write(document).Should().Be(content);
    }

    [Fact]
    public void RoundtripExtendedPlaylistPreservesCrLf()
    {
        var content = "#EXTM3U\r\n#EXTINF:-1 tvg-id=\"id1\",Channel 1\r\nhttp://example.com/1\r\n";
        var document = PlaylistParser.Parse(content);

        document.NewLine.Should().Be("\r\n");
        PlaylistWriter.Write(document).Should().Be(content);
    }

    [Fact]
    public void RoundtripHlsMasterPreservesTags()
    {
        var content = "#EXTM3U\n#EXT-X-VERSION:6\n#EXT-X-STREAM-INF:BANDWIDTH=1280000,RESOLUTION=1280x720\nmaster1.m3u8\n#EXT-X-STREAM-INF:BANDWIDTH=640000,RESOLUTION=854x480\nmaster2.m3u8";
        var document = PlaylistParser.Parse(content);

        document.DetectedKind.Should().Be(PlaylistKind.HlsMaster);
        PlaylistWriter.Write(document).Should().Be(content);
    }

    [Fact]
    public void RoundtripHlsMediaPreservesUnknownTags()
    {
        var content = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:6\n#EXT-X-MEDIA-SEQUENCE:1\n#EXTINF:6.0,First\nsegment1.ts\n#EXT-UNKNOWN:demo\n#EXTINF:6.0,Second\nsegment2.ts";
        var document = PlaylistParser.Parse(content);

        document.DetectedKind.Should().Be(PlaylistKind.HlsMedia);
        PlaylistWriter.Write(document).Should().Be(content);
        document.Diagnostics.Should().Contain(d => d.Code == "TAGINF");
    }

    [Fact]
    public void ReplaceAllUpdatesOnlyMatchingLines()
    {
        var content = "alpha\nbeta\nalpha";
        var document = PlaylistParser.Parse(content);
        var options = new FindReplaceOptions { FindText = "alpha", ReplaceText = "gamma", MatchCase = true, WholeWord = false, UseRegex = false };

        var count = FindReplaceService.ReplaceAll(document, options);
        count.Should().Be(2);
        PlaylistWriter.Write(document).Should().Be("gamma\nbeta\ngamma");
    }

    [Fact]
    public void DiagnosticsProvideCorrectSpan()
    {
        var content = "#EXTINF:9,Missing uri";
        var document = PlaylistParser.Parse(content);

        document.Diagnostics.Should().ContainSingle(d =>
            d.Code == "TAG001" &&
            d.Span.LineIndex == 0 &&
            d.Span.Start == 0 &&
            d.Span.Length == content.Length);
    }
}
