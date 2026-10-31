using Avalonia.Controls;
using DocumentTemplates.CoreLibrary;
using TU_Template.ViewModels;
using TU_Template.Views;

namespace TU_Template
{
    public class TU_DocumentTemplate : IDocumentTemplate
    {
        public ITemplateMetadata Metadata { get; } = new TemplateMetadata
        {
            Name = "ТУ",
            Description = "Генерирует ТУ документ на основе .docx шаблона.",
            Author = "Маслаков АРтем",
            Version = "1.0.0"
        };

        public Control GetEditorControl(Window parentWindow)
        {
            var viewModel = new MainViewModel(parentWindow);
            return new MainView { DataContext = viewModel }; 
        }
    }
}
