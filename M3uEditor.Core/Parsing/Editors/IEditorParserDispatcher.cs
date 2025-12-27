namespace M3uEditor.Core.Parsing.Editors;

public interface IEditorParserDispatcher
{
    object? ParseForEditor(PlaylistDocument document);

    IEditorParser<T>? GetParserFor<T>(PlaylistKind kind);
}
