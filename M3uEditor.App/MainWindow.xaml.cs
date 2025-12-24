using M3uEditor.App.ViewModels;
using M3uEditor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace M3uEditor.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // WinUI 3: DataContext musi byt na Root elementu (Grid), ne na Window.
        Root.DataContext = new DocumentHostViewModel();

        var vm = (DocumentHostViewModel)Root.DataContext;
        vm.AttachWindow(this);
        vm.DispatcherQueue = DispatcherQueue;
    }

    private DocumentHostViewModel ViewModel => (DocumentHostViewModel)Root.DataContext;

    private void OnOpenInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.OpenCommand.Execute(null);
        args.Handled = true;
    }

    private void OnSaveInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.SaveCommand.Execute(null);
        args.Handled = true;
    }

    private void OnFindInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ShowFindReplaceCommand.Execute(null);
        args.Handled = true;
    }

    private void OnReplaceInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ShowFindReplaceCommand.Execute(null);
        args.Handled = true;
    }

    private void OnFindNextInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.FindReplaceViewModel.FindNextCommand.Execute(null);
        args.Handled = true;
    }

    private void OnDiagnosticSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Diagnostic diagnostic)
        {
            ViewModel.NavigateDiagnosticCommand.Execute(diagnostic);
        }
    }
}
