using Avalonia.Controls;

namespace TU_Template.ViewModels;

public class ApplicationBViewModel : ViewModelBase
{
    private readonly Window? _parentWindow;
    private readonly Models.TemplateData _templateData;

    public ApplicationBViewModel(Window? parentWindow, Models.TemplateData templateData)
    {
        _parentWindow = parentWindow;
        _templateData = templateData;
    }
}

