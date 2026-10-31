using ReactiveUI;
using System.Reactive;

namespace TU_Template.ViewModels;

public class TemplatesOverviewViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> ShowFileSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowVariablesCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowElementsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowGroupsCommand { get; }

    public ReactiveCommand<Unit, Unit> ShowApplicationA { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationB { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationV { get; }
    public ReactiveCommand<Unit, Unit> ShowApplicationG { get; }

    public TemplatesOverviewViewModel(MainViewModel mainViewModel)
    {
        // Делегируем команды у главной VM, чтобы не дублировать логику навигации.
        ShowFileSettingsCommand = mainViewModel.ShowFileSettingsCommand;
        ShowVariablesCommand = mainViewModel.ShowVariablesCommand;
        ShowElementsCommand = mainViewModel.ShowElementsCommand;
        ShowGroupsCommand = mainViewModel.ShowGroupsCommand;
        ShowApplicationA = mainViewModel.ShowApplicationA;
        ShowApplicationB = mainViewModel.ShowApplicationB;
        ShowApplicationV = mainViewModel.ShowApplicationV;
        ShowApplicationG = mainViewModel.ShowApplicationG;
    }
}

