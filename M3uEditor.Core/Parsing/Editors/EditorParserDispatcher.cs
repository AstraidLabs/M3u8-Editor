using M3uEditor.Core;
using M3uEditor.Core.Projection;

namespace M3uEditor.Core.Parsing.Editors;

public sealed class EditorParserDispatcher : IEditorParserDispatcher
{
    private readonly IEditorParser<ProjectionResult<IptvItem>> _iptvParser;
    private readonly IEditorParser<ProjectionResult<HlsMasterVariant>> _hlsMasterParser;
    private readonly IEditorParser<HlsMediaProjection> _hlsMediaParser;

    public EditorParserDispatcher(
        IEditorParser<ProjectionResult<IptvItem>>? iptvParser = null,
        IEditorParser<ProjectionResult<HlsMasterVariant>>? hlsMasterParser = null,
        IEditorParser<HlsMediaProjection>? hlsMediaParser = null)
    {
        _iptvParser = iptvParser ?? new IptvEditorParser();
        _hlsMasterParser = hlsMasterParser ?? new HlsMasterEditorParser();
        _hlsMediaParser = hlsMediaParser ?? new HlsMediaEditorParser();
    }

    public object? ParseForEditor(PlaylistDocument document)
    {
        return GetUntypedParser(document.DetectedKind)?.ParseUntyped(document);
    }

    public IEditorParser<T>? GetParserFor<T>(PlaylistKind kind)
    {
        return kind switch
        {
            PlaylistKind.HlsMaster => _hlsMasterParser as IEditorParser<T>,
            PlaylistKind.HlsMedia => _hlsMediaParser as IEditorParser<T>,
            PlaylistKind.ExtendedM3u => _iptvParser as IEditorParser<T>,
            PlaylistKind.PlainM3u => _iptvParser as IEditorParser<T>,
            _ => null
        };
    }

    private IEditorParser? GetUntypedParser(PlaylistKind kind)
    {
        return kind switch
        {
            PlaylistKind.HlsMaster => _hlsMasterParser,
            PlaylistKind.HlsMedia => _hlsMediaParser,
            PlaylistKind.ExtendedM3u => _iptvParser,
            PlaylistKind.PlainM3u => _iptvParser,
            _ => null
        };
    }
}
