namespace M3uEditor.Core.Parsing.Editors;

public interface IEditorParser
{
    object? ParseUntyped(PlaylistDocument document);
}

public interface IEditorParser<out T> : IEditorParser
{
    new T Parse(PlaylistDocument document);
}
