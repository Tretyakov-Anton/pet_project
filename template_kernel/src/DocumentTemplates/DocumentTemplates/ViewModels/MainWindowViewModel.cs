using Avalonia.Controls;
using DocumentTemplates.CoreLibrary;
using DocumentTemplates.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace DocumentTemplates.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string? _searchText;
        private IDocumentTemplate? _selectedTemplate;

        // Все шаблоны
        private readonly ObservableCollection<IDocumentTemplate> AllTemplates = new();

        // Шаблоны для отображения
        public ObservableCollection<IDocumentTemplate> Templates { get; } = new();

        public string? SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                ApplyFilter();
            }
        }

        public IDocumentTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
                this.RaisePropertyChanged(nameof(CanOpenTemplate));
            }
        }

        public bool CanOpenTemplate => SelectedTemplate != null;

        public ReactiveCommand<Unit, Unit> OpenTemplateCommand { get; }

        public MainWindowViewModel()
        {
            // Загружаем шаблоны
            var loadedTemplates = TemplateLoader.LoadTemplates("Templates");
            foreach (var template in loadedTemplates)
            {
                AllTemplates.Add(template);
                Templates.Add(template);
            }

            OpenTemplateCommand = ReactiveCommand.Create(OpenTemplateEditor);
        }

        public void ReloadTemplates()
        {
            AllTemplates.Clear();
            Templates.Clear();

            var loadedTemplates = TemplateLoader.LoadTemplates("Templates");
            foreach (var template in loadedTemplates)
            {
                AllTemplates.Add(template);
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            Templates.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllTemplates
                : AllTemplates.Where(t => 
                    t.Metadata.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var template in filtered)
                Templates.Add(template);
        }

        private void OpenTemplateEditor()
        {
            if (SelectedTemplate == null) return;

            var window = new Window
            {
                Title = SelectedTemplate.Metadata.Name,
                Width = 850,
                Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var editorControl = SelectedTemplate.GetEditorControl(window);
            window.Content = editorControl;
            window.Show();
        }
    }
}
