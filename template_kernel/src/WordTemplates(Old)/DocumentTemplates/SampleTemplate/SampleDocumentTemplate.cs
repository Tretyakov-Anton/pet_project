using Avalonia.Controls;
using DocumentTemplates.CoreLibrary;
using SampleTemplate.ViewModels;
using SampleTemplate.Views;

namespace SampleTemplate
{
    public class SampleDocumentTemplate : IDocumentTemplate
    {
        public ITemplateMetadata Metadata { get; } = new TemplateMetadata
        {
            Name = "Договор (с выбором файла)",
            Description = "Генерирует договор на основе .docx шаблона.",
            Author = "Your Name",
            Version = "2.0.0"
        };

        public Control GetEditorControl(Window parentWindow)
        {
            var viewModel = new MainViewModel(parentWindow); // <-- используем MainViewModel
            return new MainView { DataContext = viewModel }; // <-- MainView, не FileSettingsView
        }
    }
}
