using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using M3uEditor.App.Messages;
using M3uEditor.Core;
using M3uEditor.Core.Analysis;
using M3uEditor.Core.FindReplace;
using M3uEditor.Core.Parsing;
using M3uEditor.Core.Projection;
using M3uEditor.Core.Writing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace M3uEditor.App.ViewModels;

public enum SidebarTab
{
    Explorer,
    Search,
    Problems
}

public enum BottomPanelTab
{
    Raw,
    Problems
}

public partial class DocumentHostViewModel : ObservableObject
{
    private Window? _window;
    private double _lastOpenBottomHeight = 240;

    [ObservableProperty]
    private PlaylistDocument? document;

    [ObservableProperty]
    private object? currentEditorVm;

    [ObservableProperty]
    private string? currentPath;

    [ObservableProperty]
    private PlaylistKind currentKind;

    [ObservableProperty]
    private ObservableCollection<Diagnostic> diagnostics = new();

    [ObservableProperty]
    private RawLinesViewModel rawLinesViewModel = new();

    [ObservableProperty]
    private FindReplaceViewModel findReplaceViewModel = new();

    [ObservableProperty]
    private bool isFindReplaceOpen;

    [ObservableProperty]
    private SidebarTab activeSidebarTab = SidebarTab.Explorer;

    [ObservableProperty]
    private BottomPanelTab bottomPanelTab = BottomPanelTab.Raw;

    [ObservableProperty]
    private bool isBottomPanelOpen = true;

    [ObservableProperty]
    private double bottomPanelHeight = 240;

    [ObservableProperty]
    private int focusFindRequestId;

    public DispatcherQueue? DispatcherQueue { get; set; }

    public void AttachWindow(Window window)
    {
        _window = window;
        findReplaceViewModel.Host = this;
    }

    public void LoadFromDocument(PlaylistDocument playlistDocument, string? path)
    {
        Document = playlistDocument;
        CurrentKind = playlistDocument.DetectedKind;
        CurrentPath = path;
        rawLinesViewModel.Load(playlistDocument);
        findReplaceViewModel.Document = playlistDocument;
        SwitchEditor(playlistDocument);
        RefreshDiagnostics();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (_window is null)
        {
            return;
        }

        var picker = new FileOpenPicker();
        InitializePicker(picker);
        picker.FileTypeFilter.Add(".m3u");
        picker.FileTypeFilter.Add(".m3u8");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        var buffer = await FileIO.ReadBufferAsync(file);
        using var reader = DataReader.FromBuffer(buffer);
        var bytes = new byte[buffer.Length];
        reader.ReadBytes(bytes);
        var text = Encoding.UTF8.GetString(bytes);

        var doc = PlaylistParser.Parse(text);
        doc.OriginalPath = file.Path;
        LoadFromDocument(doc, file.Path);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Document is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(CurrentPath))
        {
            await SaveAsAsync();
            return;
        }

        var content = PlaylistWriter.Write(Document);
        await FileIO.WriteTextAsync(await StorageFile.GetFileFromPathAsync(CurrentPath), content, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        Document.OriginalPath = CurrentPath;
        RefreshDiagnostics();
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (_window is null || Document is null)
        {
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedFileName = string.IsNullOrEmpty(CurrentPath) ? "playlist.m3u8" : Path.GetFileName(CurrentPath)
        };
        InitializePicker(picker);
        picker.FileTypeChoices.Add("M3U", new List<string> { ".m3u", ".m3u8" });

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        var content = PlaylistWriter.Write(Document);
        await FileIO.WriteTextAsync(file, content, Windows.Storage.Streams.UnicodeEncoding.Utf8);
        CurrentPath = file.Path;
        Document.OriginalPath = CurrentPath;
        RefreshDiagnostics();
    }

    [RelayCommand]
    private void Validate()
    {
        RefreshDiagnostics();
    }

    [RelayCommand]
    private void ShowFindReplace()
    {
        IsFindReplaceOpen = !IsFindReplaceOpen;
        if (IsFindReplaceOpen)
        {
            ActiveSidebarTab = SidebarTab.Search;
            RequestFindFocus();
        }
    }

    [RelayCommand]
    private void NavigateDiagnostic(Diagnostic? diagnostic)
    {
        if (diagnostic is null)
        {
            return;
        }

        WeakReferenceMessenger.Default.Send(new NavigateToSpanMessage(diagnostic.Span));
    }

    public void RequestFindFocus()
    {
        FocusFindRequestId++;
    }

    partial void OnIsBottomPanelOpenChanged(bool value)
    {
        if (value)
        {
            BottomPanelHeight = Math.Max(160, _lastOpenBottomHeight);
        }
        else
        {
            _lastOpenBottomHeight = BottomPanelHeight;
            BottomPanelHeight = 0;
        }
    }

    partial void OnBottomPanelHeightChanged(double value)
    {
        if (value > 0)
        {
            IsBottomPanelOpen = true;
            _lastOpenBottomHeight = value;
        }
    }

    private void InitializePicker(object picker)
    {
        if (_window is null)
        {
            return;
        }

        var hwnd = WindowNative.GetWindowHandle(_window);
        InitializeWithWindow.Initialize(picker, hwnd);
    }

    private void SwitchEditor(PlaylistDocument document)
    {
        currentEditorVm = document.DetectedKind switch
        {
            PlaylistKind.HlsMaster => new HlsMasterEditorViewModel(document),
            PlaylistKind.HlsMedia => new HlsMediaEditorViewModel(document),
            PlaylistKind.ExtendedM3u => new IptvEditorViewModel(document),
            _ => new IptvEditorViewModel(document)
        };
        OnPropertyChanged(nameof(CurrentEditorVm));
    }

    private void RefreshDiagnostics()
    {
        if (Document is null)
        {
            diagnostics = new ObservableCollection<Diagnostic>();
            OnPropertyChanged(nameof(Diagnostics));
            return;
        }

        var rendered = PlaylistWriter.Write(Document);
        var reparsed = PlaylistParser.Parse(rendered);
        Document.Diagnostics.Clear();
        Document.Diagnostics.AddRange(reparsed.Diagnostics);
        Document.DetectedKind = reparsed.DetectedKind;
        CurrentKind = reparsed.DetectedKind;
        rawLinesViewModel.Load(Document);
        Diagnostics = new ObservableCollection<Diagnostic>(PlaylistAnalyzer.Analyze(Document));
    }
}
