using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using M3uEditor.App.Messages;
using M3uEditor.Core;
using M3uEditor.Core.FindReplace;

namespace M3uEditor.App.ViewModels;

public partial class FindReplaceViewModel : ObservableObject
{
    private PlaylistDocument? _document;

    [ObservableProperty]
    private string findText = string.Empty;

    [ObservableProperty]
    private string replaceText = string.Empty;

    [ObservableProperty]
    private bool matchCase;

    [ObservableProperty]
    private bool wholeWord;

    [ObservableProperty]
    private bool useRegex;

    [ObservableProperty]
    private FindMatch? currentMatch;

    public DocumentHostViewModel? Host { get; set; }

    public PlaylistDocument? Document
    {
        get => _document;
        set
        {
            SetProperty(ref _document, value);
            CurrentMatch = null;
        }
    }

    [RelayCommand]
    private void Find()
    {
        if (Document is null || string.IsNullOrEmpty(FindText))
        {
            return;
        }

        var options = BuildOptions();
        CurrentMatch = FindReplaceService.FindNext(Document, options, null);
        if (CurrentMatch is not null)
        {
            WeakReferenceMessenger.Default.Send(new NavigateToFindMatchMessage(CurrentMatch));
        }
    }

    [RelayCommand]
    private void FindNext()
    {
        if (Document is null || string.IsNullOrEmpty(FindText))
        {
            return;
        }

        var options = BuildOptions();
        CurrentMatch = FindReplaceService.FindNext(Document, options, CurrentMatch);
        if (CurrentMatch is not null)
        {
            WeakReferenceMessenger.Default.Send(new NavigateToFindMatchMessage(CurrentMatch));
        }
    }

    [RelayCommand]
    private void Replace()
    {
        if (Document is null || CurrentMatch is null)
        {
            return;
        }

        var options = BuildOptions();
        CurrentMatch = FindReplaceService.ReplaceCurrent(Document, CurrentMatch, options);
        Host?.ValidateCommand.Execute(null);
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (Document is null || string.IsNullOrEmpty(FindText))
        {
            return;
        }

        var options = BuildOptions();
        FindReplaceService.ReplaceAll(Document, options);
        Host?.ValidateCommand.Execute(null);
    }

    private FindReplaceOptions BuildOptions() => new()
    {
        FindText = FindText,
        ReplaceText = ReplaceText,
        MatchCase = MatchCase,
        WholeWord = WholeWord,
        UseRegex = UseRegex
    };
}
