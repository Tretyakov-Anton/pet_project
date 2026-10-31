using CommunityToolkit.Mvvm.ComponentModel;
using Template_TU.Models;
using TemplatesCore;

namespace Template_TU.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private TemplateData _templateData = new();

    public MainWindowViewModel(
        MainViewModel mainViewModel,
        VariablesViewModel variablesViewModel,
        ElementsViewModel elementsViewModel, GroupsViewModel groupsViewModel)
    {
        //MainViewModel = mainViewModel;
        //VariablesViewModel = variablesViewModel;
        //ElementsViewModel = elementsViewModel;
        //GroupsViewModel = groupsViewModel;

        MainViewModel = new MainViewModel(new Lazy<Avalonia.Platform.Storage.IStorageProvider>, new Lazy<Avalonia.Controls.Notifications.IManagedNotificationManager>, new );
        VariablesViewModel = variablesViewModel;
        ElementsViewModel = elementsViewModel;
        GroupsViewModel = groupsViewModel;
    }

    public MainViewModel MainViewModel { get; }
    public VariablesViewModel VariablesViewModel { get; }
    public ElementsViewModel ElementsViewModel { get; }
    public GroupsViewModel GroupsViewModel { get; }
}