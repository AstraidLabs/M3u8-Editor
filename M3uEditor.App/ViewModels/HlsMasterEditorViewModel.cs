using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using M3uEditor.Core;
using M3uEditor.Core.Editing;
using M3uEditor.Core.Parsing.Editors;
using M3uEditor.Core.Projection;

namespace M3uEditor.App.ViewModels;

public partial class HlsMasterVariantViewModel : ObservableObject
{
    public int StreamInfLineIndex { get; }
    public int UriLineIndex { get; }

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string bandwidth = string.Empty;

    [ObservableProperty]
    private string? resolution;

    [ObservableProperty]
    private string? codecs;

    public AttributeCollection Attributes { get; }

    public HlsMasterVariantViewModel(HlsMasterVariant variant)
    {
        StreamInfLineIndex = variant.StreamInfLineIndex;
        UriLineIndex = variant.UriLineIndex;
        Url = variant.Url;
        Attributes = variant.Attributes;

        bandwidth = variant.Attributes.Find("BANDWIDTH")?.Value ?? string.Empty;
        resolution = variant.Attributes.Find("RESOLUTION")?.Value;
        codecs = variant.Attributes.Find("CODECS")?.Value;
    }
}

public partial class HlsMasterEditorViewModel : ObservableObject
{
    private readonly PlaylistDocument _document;

    [ObservableProperty]
    private ObservableCollection<HlsMasterVariantViewModel> variants = new();

    [ObservableProperty]
    private HlsMasterVariantViewModel? selectedVariant;

    public HlsMasterEditorViewModel(PlaylistDocument document, ProjectionResult<HlsMasterVariant>? projection = null)
    {
        _document = document;
        Load(document, projection);
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedVariant is null)
        {
            return;
        }

        PlaylistEditor.UpdateUri(_document, SelectedVariant.UriLineIndex, SelectedVariant.Url);

        if (!string.IsNullOrEmpty(SelectedVariant.Bandwidth))
        {
            PlaylistEditor.UpdateStreamInfAttribute(_document, SelectedVariant.StreamInfLineIndex, "BANDWIDTH", SelectedVariant.Bandwidth);
        }

        if (!string.IsNullOrEmpty(SelectedVariant.Resolution))
        {
            PlaylistEditor.UpdateStreamInfAttribute(_document, SelectedVariant.StreamInfLineIndex, "RESOLUTION", SelectedVariant.Resolution);
        }

        if (!string.IsNullOrEmpty(SelectedVariant.Codecs))
        {
            PlaylistEditor.UpdateStreamInfAttribute(_document, SelectedVariant.StreamInfLineIndex, "CODECS", SelectedVariant.Codecs);
        }
    }

    private void Load(PlaylistDocument document, ProjectionResult<HlsMasterVariant>? projection)
    {
        Variants.Clear();
        var projectionData = projection ?? new HlsMasterEditorParser().Parse(document);
        foreach (var variant in projectionData.Items)
        {
            Variants.Add(new HlsMasterVariantViewModel(variant));
        }
    }
}
