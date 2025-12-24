using M3uEditor.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace M3uEditor.App.Views;

public class EditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? IptvTemplate { get; set; }

    public DataTemplate? HlsMasterTemplate { get; set; }

    public DataTemplate? HlsMediaTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            IptvEditorViewModel => IptvTemplate,
            HlsMasterEditorViewModel => HlsMasterTemplate,
            HlsMediaEditorViewModel => HlsMediaTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
