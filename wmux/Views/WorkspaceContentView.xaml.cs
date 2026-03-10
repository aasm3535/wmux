using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class WorkspaceContentView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(WorkspaceContentView), new PropertyMetadata(null));

    public TabManagerViewModel? ViewModel
    {
        get => (TabManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public WorkspaceContentView()
    {
        InitializeComponent();
    }
}
