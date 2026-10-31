using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;

namespace TU_Template.ViewModels;
public class FileSettingsViewModel : ViewModelBase
{
    private string _sourcePath;
    public string SourcePath
    {
        get => _sourcePath;
        set => this.RaiseAndSetIfChanged(ref _sourcePath, value);
    }

    private string _resultPath;
    public string ResultPath
    {
        get => _resultPath;
        set => this.RaiseAndSetIfChanged(ref _resultPath, value);
    }

    public ReactiveCommand<Unit, Unit> SelectDocumentCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectResultCommand { get; }

    private Window _parentWindow;

    public FileSettingsViewModel(Window parentWindow)
    {
        _parentWindow = parentWindow;

        SelectDocumentCommand = ReactiveCommand.Create(SelectDocument);
        SelectResultCommand = ReactiveCommand.Create(SelectResult);
    }

    private async void SelectDocument()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите документ",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter
                {
                    Name = "Документы",
                    Extensions = { "docx" }
                }
            }
        };

        var result = await dialog.ShowAsync(_parentWindow);
        if (result != null && result.Length > 0)
        {
            SourcePath = result[0]; // Устанавливаем путь к выбранному файлу
        }
    }

    private async void SelectResult()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку для сохранения"
        };

        var result = await dialog.ShowAsync(_parentWindow);
        if (!string.IsNullOrEmpty(result))
        {
            ResultPath = result; // Устанавливаем путь к выбранной папке
        }
    }
}
