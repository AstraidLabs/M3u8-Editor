using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using M3uEditor.Core;
using M3uEditor.Core.Editing;
using M3uEditor.Core.Parsing.Editors;
using M3uEditor.Core.Projection;

namespace M3uEditor.App.ViewModels;

public partial class HlsMediaSegmentViewModel : ObservableObject
{
    public int ExtInfLineIndex { get; }
    public int UriLineIndex { get; }
    public IReadOnlyList<int> LeadingTagIndices { get; }

    [ObservableProperty]
    private double duration;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string uri = string.Empty;

    [ObservableProperty]
    private string leadingTagsDisplay = string.Empty;

    public HlsMediaSegmentViewModel(PlaylistDocument document, HlsMediaSegment segment)
    {
        ExtInfLineIndex = segment.ExtInfLineIndex;
        UriLineIndex = segment.UriLineIndex;
        LeadingTagIndices = segment.LeadingTagIndices;
        Duration = segment.Duration ?? 0;
        Title = segment.Title;
        Uri = segment.Uri;
        leadingTagsDisplay = BuildTagLabel(document, segment.LeadingTagIndices);
    }

    private static string BuildTagLabel(PlaylistDocument document, IEnumerable<int> indices)
    {
        var names = new List<string>();
        foreach (var index in indices)
        {
            if (index >= 0 && index < document.Lines.Count && document.Lines[index] is TagLine tag)
            {
                names.Add(tag.TagName);
            }
        }

        return string.Join(", ", names);
    }
}

public partial class HlsMediaEditorViewModel : ObservableObject
{
    private readonly PlaylistDocument _document;

    [ObservableProperty]
    private ObservableCollection<HlsMediaSegmentViewModel> segments = new();

    [ObservableProperty]
    private ObservableCollection<string> headerTags = new();

    [ObservableProperty]
    private HlsMediaSegmentViewModel? selectedSegment;

    public HlsMediaEditorViewModel(PlaylistDocument document, HlsMediaProjection? projection = null)
    {
        _document = document;
        Load(document, projection);
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedSegment is null)
        {
            return;
        }

        PlaylistEditor.UpdateUri(_document, SelectedSegment.UriLineIndex, SelectedSegment.Uri);
        PlaylistEditor.UpdateHlsExtInf(_document, SelectedSegment.ExtInfLineIndex, SelectedSegment.Duration, SelectedSegment.Title);
    }

    private void Load(PlaylistDocument document, HlsMediaProjection? projection)
    {
        Segments.Clear();
        HeaderTags.Clear();

        var projectionData = projection ?? new HlsMediaEditorParser().Parse(document);
        foreach (var headerIndex in projectionData.HeaderTagIndices)
        {
            if (headerIndex >= 0 && headerIndex < document.Lines.Count)
            {
                HeaderTags.Add(document.Lines[headerIndex].Raw);
            }
        }

        foreach (var segment in projectionData.Segments.Items)
        {
            Segments.Add(new HlsMediaSegmentViewModel(document, segment));
        }
    }
}
