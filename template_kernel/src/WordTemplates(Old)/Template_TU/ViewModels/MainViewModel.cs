using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Template_TU.Models;
using Template_TU.Services;
using TemplatesCore;
using Xceed.Words.NET;

namespace Template_TU.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private static readonly FilePickerSaveOptions TemplateFilePickerSaveOptions = new()
    {
        FileTypeChoices =
        [
            new("json")
            {
                Patterns = [ "*.json" ],
                MimeTypes = [ "application/json" ]
            }
        ],
        SuggestedFileName = "Config.json"
    };

    private static readonly FilePickerOpenOptions TemplateFilePickerOpenOptions = new()
    {
        FileTypeFilter =
        [
            new("json")
            {
                Patterns = [ "*.json" ],
                MimeTypes = [ "application/json" ]
            }
        ],
        AllowMultiple = false
    };

    private static readonly FilePickerSaveOptions DocumentFilePickerSaveOptions = new()
    {
        FileTypeChoices =
        [
            new("docx")
            {
                Patterns = [ "*.docx" ],
            }
        ],
        SuggestedFileName = "Config.json"
    };

    private static readonly FilePickerOpenOptions DocumentFilePickerOpenOptions = new()
    {
        FileTypeFilter =
        [
            new("docx")
            {
                Patterns = [ "*.docx" ],
            }
        ],
        AllowMultiple = false
    };

    private readonly Lazy<IStorageProvider> _storageProvider;
    private readonly Lazy<IManagedNotificationManager> _managedNotificationManager;
    private readonly IDocumentProcessor _documentProcessor;

    [ObservableProperty] private string _sourcePath = "template.docx";
    [ObservableProperty] private string _resultPath = "Output.docx";
    //---
    [ObservableProperty]
    private ObservableCollection<string> _templates;

    [ObservableProperty]
    private string _selectedTemplate;
    //---
    public MainViewModel(
        Lazy<IStorageProvider> storageProvider,
        Lazy<IManagedNotificationManager> managedNotificationManager,
        IDocumentProcessor documentProcessor,
        TemplateData templateData)
    {
        TemplateData = templateData;
        _storageProvider = storageProvider;
        _managedNotificationManager = managedNotificationManager;
        _documentProcessor = documentProcessor;
    }

    public TemplateData TemplateData { get; }

    [RelayCommand]
    private async Task Load()
    {
        try
        {
            var result = await _storageProvider.Value.OpenFilePickerAsync(TemplateFilePickerOpenOptions);
            if (result.Count == 0) return;
            await using var stream = await result[0].OpenReadAsync();
            var data = await JsonSerializer.DeserializeAsync<TemplateDataJsonModel>(stream);// тут происходит считываение с шаблона
            if (data != null) TemplateData.CopyFrom(data.ToTemplateData());

            _managedNotificationManager.Value.Show(
                new Notification("Конфигурация загружена", "",
                    NotificationType.Success, TimeSpan.FromSeconds(2)));
        }
        catch (Exception e)
        {
            _managedNotificationManager.Value.Show(
                new Notification("Ошибка", e.Message, NotificationType.Error, TimeSpan.FromSeconds(5)));
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var result = await _storageProvider.Value.SaveFilePickerAsync(TemplateFilePickerSaveOptions);
            if (result == null) return;
            await using var stream = await result.OpenWriteAsync();
            await JsonSerializer.SerializeAsync(stream, TemplateData.ToJsonModel(), new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            });

            _managedNotificationManager.Value.Show(
                new Notification("Конфигурация сохранена", "",
                    NotificationType.Success, TimeSpan.FromSeconds(2)));
        }
        catch (Exception e)
        {
            _managedNotificationManager.Value.Show(
                new Notification("Ошибка", e.Message, NotificationType.Error, TimeSpan.FromSeconds(5)));
        }
    }

    [RelayCommand]
    private async Task SelectDocument()
    {
        var result = await _storageProvider.Value.OpenFilePickerAsync(DocumentFilePickerOpenOptions);
        if (result.Count == 0) return;
        SourcePath = result[0].TryGetLocalPath()!;
    }

    [RelayCommand]
    private async Task SelectResult()
    {
        var result = await _storageProvider.Value.SaveFilePickerAsync(DocumentFilePickerSaveOptions);
        if (result == null) return;
        ResultPath = result.TryGetLocalPath()!;
    }

    [RelayCommand]
    private async Task Create()// Метод который запускает весь процесс
    {
        try
        {
            await Task.Run(() =>
            {
                FileStream input = new(SourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var document = DocX.Load(input);
                input.Dispose();

                _documentProcessor.Process(document, TemplateData);// Тут вызывается метод который запускает обработку файла по шаблону

                document.SaveAs(ResultPath);// Сохранение файла
            });

            _managedNotificationManager.Value.Show(
                new Notification("Документ сохранён", Path.GetFullPath(ResultPath),
                    NotificationType.Success, TimeSpan.FromSeconds(3)));
        }
        catch (Exception e)
        {
            _managedNotificationManager.Value.Show(
                new Notification("Ошибка", e.Message, NotificationType.Error, TimeSpan.FromSeconds(5)));
        }
    }
}