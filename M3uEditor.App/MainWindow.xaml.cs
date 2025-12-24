using M3uEditor.App.ViewModels;
using M3uEditor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace M3uEditor.App;

public sealed partial class MainWindow : Window
{
    public DocumentHostViewModel ViewModel => (DocumentHostViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel.AttachWindow(this);
        ViewModel.DispatcherQueue = DispatcherQueue;
        ConfigureKeyboardAccelerators();
    }

    private void ConfigureKeyboardAccelerators()
    {
        KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.F, Modifiers = VirtualKeyModifiers.Control, Invoked = (_, __) => ViewModel.ShowFindReplaceCommand.Execute(null) });
        KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.H, Modifiers = VirtualKeyModifiers.Control, Invoked = (_, __) => ViewModel.ShowFindReplaceCommand.Execute(null) });
        KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.F3, Invoked = (_, __) => ViewModel.FindReplaceViewModel.FindNextCommand.Execute(null) });
        KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.O, Modifiers = VirtualKeyModifiers.Control, Invoked = (_, __) => ViewModel.OpenCommand.Execute(null) });
        KeyboardAccelerators.Add(new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control, Invoked = (_, __) => ViewModel.SaveCommand.Execute(null) });
    }

    private void OnDiagnosticSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Diagnostic diagnostic)
        {
            ViewModel.NavigateDiagnosticCommand.Execute(diagnostic);
        }
    }
}
