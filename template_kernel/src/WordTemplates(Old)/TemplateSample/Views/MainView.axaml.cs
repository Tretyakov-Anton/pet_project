using Avalonia.Controls;
using TemplateSample.ViewModels;

namespace TemplateSample.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}