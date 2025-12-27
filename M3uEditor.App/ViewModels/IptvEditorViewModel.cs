using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using M3uEditor.Core;
using M3uEditor.Core.Editing;
using M3uEditor.Core.Parsing.Editors;
using M3uEditor.Core.Projection;

namespace M3uEditor.App.ViewModels;

public partial class IptvItemViewModel : ObservableObject
{
    public int UriLineIndex { get; }
    public int? ExtInfLineIndex { get; }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private int duration = -1;

    [ObservableProperty]
    private string? tvgId;

    [ObservableProperty]
    private string? tvgName;

    [ObservableProperty]
    private string? tvgLogo;

    [ObservableProperty]
    private string? groupTitle;

    public AttributeCollection Attributes { get; }

    public IptvItemViewModel(IptvItem item, int durationValue)
    {
        UriLineIndex = item.UriLineIndex;
        ExtInfLineIndex = item.ExtInfLineIndex;
        Url = item.Url;
        Attributes = item.Attributes;
        Duration = durationValue;
        Title = item.Title;
        tvgId = item.GetAttribute("tvg-id");
        tvgName = item.GetAttribute("tvg-name");
        tvgLogo = item.GetAttribute("tvg-logo");
        groupTitle = item.GetAttribute("group-title");
    }
}

public partial class IptvEditorViewModel : ObservableObject
{
    private readonly PlaylistDocument _document;

    [ObservableProperty]
    private ObservableCollection<IptvItemViewModel> items = new();

    [ObservableProperty]
    private IptvItemViewModel? selectedItem;

    public IptvEditorViewModel(PlaylistDocument document, ProjectionResult<IptvItem>? projection = null)
    {
        _document = document;
        LoadItems(document, projection);
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in SelectedItem.Attributes)
        {
            attributes[entry.Name] = entry.Value;
        }

        if (!string.IsNullOrEmpty(SelectedItem.TvgId))
        {
            attributes["tvg-id"] = SelectedItem.TvgId;
        }

        if (!string.IsNullOrEmpty(SelectedItem.TvgName))
        {
            attributes["tvg-name"] = SelectedItem.TvgName;
        }

        if (!string.IsNullOrEmpty(SelectedItem.TvgLogo))
        {
            attributes["tvg-logo"] = SelectedItem.TvgLogo;
        }

        if (!string.IsNullOrEmpty(SelectedItem.GroupTitle))
        {
            attributes["group-title"] = SelectedItem.GroupTitle;
        }

        PlaylistEditor.UpdateUri(_document, SelectedItem.UriLineIndex, SelectedItem.Url);
        PlaylistEditor.UpdateIptvMetadata(_document, SelectedItem.UriLineIndex, SelectedItem.Duration, SelectedItem.Title, attributes);
    }

    private void LoadItems(PlaylistDocument document, ProjectionResult<IptvItem>? projection)
    {
        Items.Clear();
        var projectionData = projection ?? new IptvEditorParser().Parse(document);
        foreach (var item in projectionData.Items)
        {
            var duration = item.ExtInfLineIndex is int extIndex && document.Lines[extIndex] is TagLine tagLine
                ? ParseDuration(tagLine.TagValue)
                : -1;
            Items.Add(new IptvItemViewModel(item, duration));
        }
    }

    private int ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return -1;
        }

        var commaIndex = value.IndexOf(',');
        var head = commaIndex >= 0 ? value[..commaIndex] : value;
        return int.TryParse(head.Split(' ', 2)[0], out var duration) ? duration : -1;
    }
}
