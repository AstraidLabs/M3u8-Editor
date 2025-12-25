using M3uEditor.App.ViewModels;
using M3uEditor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace M3uEditor.App;

public sealed partial class MainWindow : Window
{
    private readonly DocumentHostViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // WinUI 3: DataContext musi byt na Root elementu (Grid), ne na Window.
        _viewModel = new DocumentHostViewModel();
        if (Content is FrameworkElement root)
        {
            root.DataContext = _viewModel;
        }

        _viewModel.AttachWindow(this);
        _viewModel.DispatcherQueue = DispatcherQueue;
    }

    private DocumentHostViewModel ViewModel => _viewModel;

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
        ViewModel.ActiveSidebarTab = SidebarTab.Search;
        ViewModel.IsFindReplaceOpen = true;
        ViewModel.RequestFindFocus();
        args.Handled = true;
    }

    private void OnReplaceInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ActiveSidebarTab = SidebarTab.Search;
        ViewModel.IsFindReplaceOpen = true;
        ViewModel.RequestFindFocus();
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

    private void OnDiagnosticClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Diagnostic diagnostic)
        {
            ViewModel.NavigateDiagnosticCommand.Execute(diagnostic);
        }
    }

    private void OnToggleBottomPanel(object sender, RoutedEventArgs e)
    {
        ViewModel.IsBottomPanelOpen = !ViewModel.IsBottomPanelOpen;
    }

    private void OnBottomSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (BottomPanelContainer is FrameworkElement element)
        {
            ViewModel.BottomPanelHeight = element.ActualHeight;
        }
    }
}
