using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using M3uEditor.App.Messages;
using M3uEditor.Core;
using M3uEditor.Core.FindReplace;
using Microsoft.UI.Xaml.Documents;

namespace M3uEditor.App.ViewModels;

public partial class RawLineDisplay : ObservableObject
{
    [ObservableProperty]
    private int lineNumber;

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private IList<TextRange> highlights = new List<TextRange>();
}

public partial class RawLinesViewModel : ObservableRecipient
{
    [ObservableProperty]
    private int? selectedIndex;

    public ObservableCollection<RawLineDisplay> Lines { get; } = new();

    public RawLinesViewModel()
    {
        Messenger.Register<NavigateToSpanMessage>(this, (_, message) =>
        {
            HighlightSpan(message.Value.LineIndex, message.Value.Start, message.Value.Length);
        });

        Messenger.Register<NavigateToFindMatchMessage>(this, (_, message) =>
        {
            HighlightSpan(message.Value.LineIndex, message.Value.Start, message.Value.Length);
        });
    }

    public void Load(PlaylistDocument document)
    {
        Lines.Clear();
        for (var i = 0; i < document.Lines.Count; i++)
        {
            Lines.Add(new RawLineDisplay
            {
                LineNumber = document.Lines[i].LineNumber,
                Text = document.Lines[i].Raw
            });
        }
    }

    private void HighlightSpan(int lineIndex, int start, int length)
    {
        if (lineIndex < 0 || lineIndex >= Lines.Count)
        {
            return;
        }

        var textLength = Lines[lineIndex].Text.Length;
        if (start < 0 || start >= textLength)
        {
            start = 0;
        }

        length = Math.Max(0, Math.Min(length, textLength - start));

        for (var i = 0; i < Lines.Count; i++)
        {
            Lines[i].Highlights = i == lineIndex && length > 0
                ? new List<TextRange> { new(start, length) }
                : new List<TextRange>();
        }

        SelectedIndex = lineIndex;
    }
}
