using CommunityToolkit.Mvvm.Input;
using Template_TU.Models;
using TemplatesCore;

namespace Template_TU.ViewModels;

public partial class ElementsViewModel : ViewModelBase
{
    public ElementsViewModel(TemplateData templateData)
    {
        TemplateData = templateData;
    }

    public TemplateData TemplateData { get; }

    [RelayCommand]
    private void Add()
    {
        TemplateData.Elements.Add(new Element());
    }

    [RelayCommand]
    private void Remove(Element e)
    {
        TemplateData.Elements.Remove(e);
    }
}