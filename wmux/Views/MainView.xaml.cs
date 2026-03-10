using Microsoft.UI.Xaml.Controls;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class MainView : UserControl
{
    public TabManagerViewModel ViewModel { get; } = new();

    public MainView()
    {
        InitializeComponent();
        Loaded += (_, _) => ViewModel.Initialize();
    }
}
