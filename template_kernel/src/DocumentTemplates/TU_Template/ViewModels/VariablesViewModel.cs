using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public partial class VariablesViewModel : ViewModelBase
{
    private readonly Window _parentWindow;

    public VariablesViewModel(Window parentWindow, TemplateData templateData)
    {
        _parentWindow = parentWindow;
        TemplateData = templateData;

        // Обеспечим наличие заранее определённых переменных
        TemplateData.EnsureVariables(PredefinedVariables.Names);
    }

    public TemplateData TemplateData { get; }

    [RelayCommand]
    private void Add()
    {
        TemplateData.Variables.Add(new Variable("", ""));
    }

    [RelayCommand]
    private void Remove(Variable v)
    {
        TemplateData.Variables.Remove(v);
    }
}

