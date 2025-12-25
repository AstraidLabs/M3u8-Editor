using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace M3uEditor.App.Views;

public sealed partial class FindReplacePanel : UserControl
{
    public static readonly DependencyProperty FocusRequestIdProperty =
        DependencyProperty.Register(nameof(FocusRequestId), typeof(int), typeof(FindReplacePanel), new PropertyMetadata(0, OnFocusRequestChanged));

    public FindReplacePanel()
    {
        InitializeComponent();
    }

    public int FocusRequestId
    {
        get => (int)GetValue(FocusRequestIdProperty);
        set => SetValue(FocusRequestIdProperty, value);
    }

    public void FocusFind()
    {
        _ = FindTextBox?.Focus(FocusState.Keyboard);
    }

    private static void OnFocusRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindReplacePanel panel)
        {
            panel.FocusFind();
        }
    }
}
