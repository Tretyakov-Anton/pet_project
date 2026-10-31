using CommunityToolkit.Mvvm.Input;
using Template_TU.Models;
using TemplatesCore;

namespace Template_TU.ViewModels;

public partial class VariablesViewModel : ViewModelBase
{
    public VariablesViewModel(TemplateData templateData)
    {
        TemplateData = templateData;
    }

    public TemplateData TemplateData { get; }

    [RelayCommand]
    private void Add()
    {
        TemplateData.Variables.Add(new("",""));
    }

    [RelayCommand]
    private void Remove(Variable v)
    {
        TemplateData.Variables.Remove(v);
    }
}