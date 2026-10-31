using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public class MainViewModel : ReactiveObject
{
    public TemplateData TemplateData { get; set; }
    private string _activeMenu = "Overview";
    public string ActiveMenu
    {
        get => _activeMenu;
        set => this.RaiseAndSetIfChanged(ref _activeMenu, value);
    }

    public ReactiveCommand<Unit, Unit> ShowFileSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowElementsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowGroupsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationG { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationA { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationB { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationV { get; }
    public ReactiveCommand<Unit, Unit> ShowVariablesCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    private readonly FileSettingsViewModel _fileSettingsViewModel;

    /// <summary>
    /// Настройки проекта — выставлены наружу, чтобы пометку о несохранённых изменениях
    /// было видно на любой вкладке, а не только на вкладке проекта.
    /// </summary>
    public FileSettingsViewModel FileSettings => _fileSettingsViewModel;
    private readonly ElementsViewModel _elementsViewModel;
    private readonly GroupsViewModel _groupsViewModel;
    private readonly ApplicationGViewModel _applicationGViewModel;
    private readonly ApplicationAViewModel _applicationAViewModel;
    private readonly ApplicationBViewModel _applicationBViewModel;
    private readonly ApplicationVViewModel _applicationVViewModel;
    private readonly VariablesViewModel _variablesViewModel;

    private ViewModelBase _currentView;
    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public MainViewModel(Window parentWindow)
    {
        TemplateData = new TemplateData();

        // Создаём один экземпляр каждого под-ViewModel, чтобы состояние (включая пути и имя проекта)
        // сохранялось при переключении вкладок.
        _fileSettingsViewModel = new FileSettingsViewModel(parentWindow, TemplateData);
        _elementsViewModel = new ElementsViewModel(parentWindow, TemplateData);
        _groupsViewModel = new GroupsViewModel(parentWindow, TemplateData);
        _applicationGViewModel = new ApplicationGViewModel(parentWindow, TemplateData);
        _applicationAViewModel = new ApplicationAViewModel(parentWindow, TemplateData);
        _applicationBViewModel = new ApplicationBViewModel(parentWindow, TemplateData);
        _applicationVViewModel = new ApplicationVViewModel(parentWindow, TemplateData);
        _variablesViewModel = new VariablesViewModel(parentWindow, TemplateData);

        ShowFileSettingsCommand = ReactiveCommand.Create(() =>
        {
            CurrentView = _fileSettingsViewModel;
            ActiveMenu = "FileSettings";
        });
        ShowElementsCommand = ReactiveCommand.Create(() =>
        {
            CurrentView = _elementsViewModel;
            ActiveMenu = "Elements";
        });
        ShowGroupsCommand = ReactiveCommand.Create(() =>
        {
            CurrentView = _groupsViewModel;
            ActiveMenu = "Groups";
        });
        ShowApplicationG = ReactiveCommand.Create(() =>
        {
            CurrentView = _applicationGViewModel;
            ActiveMenu = "ApplicationG";
        });
        ShowApplicationA = ReactiveCommand.Create(() =>
        {
            CurrentView = _applicationAViewModel;
            ActiveMenu = "ApplicationA";
        });
        ShowApplicationB = ReactiveCommand.Create(() =>
        {
            CurrentView = _applicationBViewModel;
            ActiveMenu = "ApplicationB";
        });
        ShowApplicationV = ReactiveCommand.Create(() =>
        {
            // Do not recreate VM on tab switch; keep user selections.
            CurrentView = _applicationVViewModel;
            ActiveMenu = "ApplicationV";
        });
        ShowVariablesCommand = ReactiveCommand.Create(() =>
        {
            CurrentView = _variablesViewModel;
            ActiveMenu = "Variables";
        });

        BackCommand = ReactiveCommand.Create(() =>
        {
            CurrentView = new TemplatesOverviewViewModel(this);
            ActiveMenu = "Overview";
        });

        CurrentView = new TemplatesOverviewViewModel(this);

        // Все разделы созданы и успели досоздать свои значения по умолчанию —
        // только теперь считаем проект "чистым".
        _fileSettingsViewModel.ResetChangeState();
    }
}