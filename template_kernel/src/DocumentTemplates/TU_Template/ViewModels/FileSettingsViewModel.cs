using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Xceed.Words.NET;
using TU_Template.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace TU_Template.ViewModels;

public class FileSettingsViewModel : ViewModelBase
{
    private readonly Window _parentWindow;
    private readonly TemplateData _templateData;
    private readonly Services.TemplateDataChangeTracker _changeTracker;
    private readonly DispatcherTimer _autoSaveTimer;

    /// <summary>Пауза после последнего изменения, по истечении которой проект сохраняется сам.</summary>
    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions ProjectJsonOptions = new()
    {
        WriteIndented = true,
        // Без этого кириллица в файле проекта превращается в АБ… и файл нечитаем.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string _projectName = "";
    public string ProjectName
    {
        get => _projectName;
        set => this.RaiseAndSetIfChanged(ref _projectName, value);
    }

    private string _sourcePath = "";
    public string SourcePath
    {
        get => _sourcePath;
        set => this.RaiseAndSetIfChanged(ref _sourcePath, value);
    }

    private string _resultPath = "";
    public string ResultPath
    {
        get => _resultPath;
        set => this.RaiseAndSetIfChanged(ref _resultPath, value);
    }

    /// <summary>Файл проекта (.json), в который идёт автосохранение. Пусто — проект ещё не сохраняли.</summary>
    private string _projectFilePath = "";
    public string ProjectFilePath
    {
        get => _projectFilePath;
        private set
        {
            this.RaiseAndSetIfChanged(ref _projectFilePath, value);
            this.RaisePropertyChanged(nameof(ProjectFileDisplay));
        }
    }

    public string ProjectFileDisplay => string.IsNullOrWhiteSpace(ProjectFilePath)
        ? "Проект ещё не сохранён в файл"
        : $"Файл проекта: {ProjectFilePath}";

    private bool _hasUnsavedChanges;
    /// <summary>Есть ли изменения, не записанные в файл проекта.</summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
            this.RaisePropertyChanged(nameof(SaveStateText));
        }
    }

    public string SaveStateText => HasUnsavedChanges
        ? "● Есть несохранённые изменения"
        : "✓ Все изменения сохранены";

    private string _statusText = "";
    /// <summary>Сообщение для пользователя: что получилось, что нет и куда записан файл.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private string _lastDocumentPath = "";
    /// <summary>Последний созданный документ Word — чтобы его можно было сразу открыть.</summary>
    public string LastDocumentPath
    {
        get => _lastDocumentPath;
        private set
        {
            this.RaiseAndSetIfChanged(ref _lastDocumentPath, value);
            this.RaisePropertyChanged(nameof(HasLastDocument));
        }
    }

    public bool HasLastDocument => !string.IsNullOrWhiteSpace(LastDocumentPath) && File.Exists(LastDocumentPath);

    public ReactiveCommand<Unit, Unit> SelectSourceFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectResultFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDocumentCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowDocumentInFolderCommand { get; }

    public FileSettingsViewModel(Window parentWindow, TemplateData templateData)
    {
        _parentWindow = parentWindow;
        _templateData = templateData;

        _templateData.EnsureVariables(TU_Template.PredefinedVariables.Names);

        SelectSourceFileCommand = ReactiveCommand.CreateFromTask(SelectSourceFile);
        SelectResultFolderCommand = ReactiveCommand.CreateFromTask(SelectResultFolder);

        CreateCommand = ReactiveCommand.CreateFromTask(CreateDocumentAsync);
        LoadCommand = ReactiveCommand.CreateFromTask(LoadProjectAsync);
        SaveCommand = ReactiveCommand.CreateFromTask(SaveProjectAsync);
        OpenDocumentCommand = ReactiveCommand.Create(OpenLastDocument);
        ShowDocumentInFolderCommand = ReactiveCommand.Create(ShowLastDocumentInFolder);

        CreateCommand.ThrownExceptions.Subscribe(ex => Fail("Ошибка создания документа", ex));
        LoadCommand.ThrownExceptions.Subscribe(ex => Fail("Ошибка открытия проекта", ex));
        SaveCommand.ThrownExceptions.Subscribe(ex => Fail("Ошибка сохранения проекта", ex));
        OpenDocumentCommand.ThrownExceptions.Subscribe(ex => Fail("Не удалось открыть документ", ex));
        ShowDocumentInFolderCommand.ThrownExceptions.Subscribe(ex => Fail("Не удалось показать папку", ex));

        // Автосохранение: ждём паузу после последнего изменения, чтобы не писать файл на каждую букву.
        _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveDelay };
        _autoSaveTimer.Tick += (_, _) =>
        {
            _autoSaveTimer.Stop();
            AutoSave();
        };

        _changeTracker = new Services.TemplateDataChangeTracker(_templateData);
        _changeTracker.Changed += (_, _) => OnDataChanged();

        // Имя проекта и пути тоже входят в файл проекта.
        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ProjectName) or nameof(SourcePath) or nameof(ResultPath))
                OnDataChanged();
        };
    }

    /// <summary>
    /// Сбрасывает пометку об изменениях. Вызывается после того, как все разделы создались:
    /// их конструкторы сами досоздают переменную «ТУ» и предустановленные термины, и без
    /// этого сброса приложение стартовало бы с надписью «есть несохранённые изменения»
    /// и сразу писало черновик, хотя пользователь ничего не вводил.
    /// </summary>
    public void ResetChangeState()
    {
        _autoSaveTimer.Stop();
        _changeTracker.Rescan();
        HasUnsavedChanges = false;
        StatusText = "";
    }

    // ------------------------------------------------------------------ выбор путей

    private async Task SelectSourceFile()
    {
        var topLevel = TopLevel.GetTopLevel(_parentWindow);
        if (topLevel == null) return;

        var options = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Выберите файл шаблона Word (.docx)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Документы Word")
                {
                    Patterns = new[] { "*.docx" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
                },
                new Avalonia.Platform.Storage.FilePickerFileType("Все файлы")
                {
                    Patterns = new[] { "*" },
                    MimeTypes = new[] { "*/*" }
                }
            }
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (result?.Count > 0)
            SourcePath = result[0].Path.LocalPath;
    }

    private async Task SelectResultFolder()
    {
        var topLevel = TopLevel.GetTopLevel(_parentWindow);
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Выберите папку, куда сохранять готовые документы",
                AllowMultiple = false
            });

        if (result?.Count > 0)
            ResultPath = result[0].Path.LocalPath;
    }

    // ------------------------------------------------------------------ создание документа

    private async Task CreateDocumentAsync()
    {
        if (string.IsNullOrWhiteSpace(SourcePath) || !File.Exists(SourcePath))
        {
            Fail("Не выбран файл шаблона Word. Укажите его в поле «Шаблон документа».");
            return;
        }

        if (string.IsNullOrWhiteSpace(ResultPath) || !Directory.Exists(ResultPath))
        {
            Fail("Не выбрана папка для готовых документов. Укажите её в поле «Папка для готовых документов».");
            return;
        }

        var sourcePath = SourcePath;
        var resultDir = ResultPath;
        var documentPath = BuildDocumentPath(resultDir, ProjectName);

        StatusText = "Создание документа…";

        // Тяжёлую работу с документом выносим из UI-потока, данные только читаем.
        var error = await Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(resultDir);

                using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var document = DocX.Load(input);

                var processor = new Services.DocumentProcessing.DocumentProcessor();
                processor.Process(document, _templateData);

                document.SaveAs(documentPath);

                // DocX не заходит в VML-копию ГОСТ-рамки в колонтитулах — дописываем её вручную,
                // иначе на каждой странице в штампе остаётся "${ТУ}" вместо номера.
                Services.DocumentProcessing.HeaderFooterVariablePatcher
                    .Patch(documentPath, processor.ResolvedVariables);

                return (string?)null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        });

        if (error != null)
        {
            Fail("Не удалось создать документ: " + error);
            return;
        }

        LastDocumentPath = documentPath;
        StatusText = $"Документ создан: {Path.GetFileName(documentPath)}. Нажмите «Открыть созданный документ».";
        Log($"Документ сохранён: {documentPath}");

        TryRevealInExplorer(documentPath);
    }

    /// <summary>
    /// Имя готового документа повторяет название проекта. Если такой файл уже есть,
    /// к имени добавляется номер: «Проект (2).docx», «Проект (3).docx» и так далее.
    /// Раньше имя было вида Generated_20250412_18_30_02.docx, и найти документ было тяжело.
    /// </summary>
    private static string BuildDocumentPath(string directory, string projectName)
    {
        var baseName = SanitizeFileName(projectName);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Документ";

        var candidate = Path.Combine(directory, baseName + ".docx");
        for (int index = 2; File.Exists(candidate); index++)
            candidate = Path.Combine(directory, $"{baseName} ({index}).docx");

        return candidate;
    }

    private static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return cleaned.Trim().TrimEnd('.');
    }

    private void OpenLastDocument()
    {
        if (!HasLastDocument)
        {
            Fail("Документ ещё не создан.");
            return;
        }

        Process.Start(new ProcessStartInfo(LastDocumentPath) { UseShellExecute = true });
    }

    private void ShowLastDocumentInFolder()
    {
        if (!HasLastDocument)
        {
            Fail("Документ ещё не создан.");
            return;
        }

        TryRevealInExplorer(LastDocumentPath);
    }

    private static void TryRevealInExplorer(string filePath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
                {
                    UseShellExecute = true
                });
            }
        }
        catch (Exception)
        {
            // Открытие проводника — вспомогательное действие, документ уже сохранён.
        }
    }

    // ------------------------------------------------------------------ проект: открыть / сохранить

    private async Task LoadProjectAsync()
    {
        var topLevel = TopLevel.GetTopLevel(_parentWindow);
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Откройте файл проекта (.json)",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Файлы проекта") { Patterns = new[] { "*.json" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Все файлы") { Patterns = new[] { "*" } }
                }
            });

        if (result == null || result.Count == 0) return;

        var filePath = result[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Fail("Выбран некорректный файл проекта.");
            return;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            Fail("Не удалось прочитать файл проекта: " + ex.Message);
            return;
        }

        // ВАЖНО: данные применяем в UI-потоке.
        // Раньше весь блок выполнялся в Task.Run, и CopyFrom менял привязанные к интерфейсу
        // коллекции из фонового потока — Avalonia бросала исключение, оно гасилось в catch,
        // и после «Загрузить» элементы и группы оказывались пустыми.
        try
        {
            TemplateProjectJsonModel? project = null;
            try { project = JsonSerializer.Deserialize<TemplateProjectJsonModel>(json); }
            catch { /* попробуем старый формат ниже */ }

            if (project is { TemplateData: not null })
            {
                var loaded = project.TemplateData.ToTemplateData();
                RestoreDeviceGroupTypesFromRawJson(json, loaded);
                _templateData.CopyFrom(loaded);
                _templateData.EnsureVariables(TU_Template.PredefinedVariables.Names);

                ProjectName = !string.IsNullOrWhiteSpace(project.Name)
                    ? project.Name
                    : Path.GetFileNameWithoutExtension(filePath);

                if (!string.IsNullOrWhiteSpace(project.SourcePath)) SourcePath = project.SourcePath;
                if (!string.IsNullOrWhiteSpace(project.ResultPath)) ResultPath = project.ResultPath;
            }
            else
            {
                // Старый формат: только TemplateDataJsonModel, без путей и имени.
                var model = JsonSerializer.Deserialize<TemplateDataJsonModel>(json);
                if (model == null)
                {
                    Fail("Не удалось разобрать файл проекта.");
                    return;
                }

                var loadedData = model.ToTemplateData();
                RestoreDeviceGroupTypesFromRawJson(json, loadedData);
                _templateData.CopyFrom(loadedData);
                _templateData.EnsureVariables(TU_Template.PredefinedVariables.Names);
                ProjectName = Path.GetFileNameWithoutExtension(filePath);
            }
        }
        catch (Exception ex)
        {
            Fail("Не удалось применить данные проекта: " + ex.Message);
            return;
        }

        ProjectFilePath = filePath;
        _changeTracker.Rescan();

        var elements = ((System.Collections.Generic.IList<Element>)_templateData.Elements).Count;
        MarkSaved($"Проект открыт: {Path.GetFileName(filePath)} — групп: {_templateData.Groups.Count}, элементов: {elements}.");
    }

    private async Task SaveProjectAsync()
    {
        var topLevel = TopLevel.GetTopLevel(_parentWindow);
        if (topLevel == null) return;

        var suggested = SanitizeFileName(ProjectName);
        if (string.IsNullOrWhiteSpace(suggested)) suggested = "Проект";

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Сохранить проект",
                SuggestedFileName = suggested + ".json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Файлы проекта") { Patterns = new[] { "*.json" } }
                }
            });

        if (file == null) return;

        var path = file.Path.LocalPath;

        if (!WriteProjectFile(path, out var error))
        {
            Fail("Не удалось сохранить проект: " + error);
            return;
        }

        ProjectFilePath = path;
        MarkSaved($"Проект сохранён: {Path.GetFileName(path)}");
    }

    // ------------------------------------------------------------------ автосохранение

    private void OnDataChanged()
    {
        HasUnsavedChanges = true;

        // Сбрасываем отсчёт: сохраняем через паузу после последнего изменения.
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void AutoSave()
    {
        if (!HasUnsavedChanges) return;

        var path = string.IsNullOrWhiteSpace(ProjectFilePath)
            ? BuildRecoveryPath()
            : ProjectFilePath;

        if (string.IsNullOrWhiteSpace(path)) return;

        if (!WriteProjectFile(path, out var error))
        {
            // Не сбрасываем пометку: пользователь должен видеть, что данные не записаны.
            StatusText = $"Автосохранение не удалось ({error}). Нажмите «Сохранить проект».";
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjectFilePath))
        {
            // Проект ещё не сохраняли явно — пишем копию для восстановления и честно говорим куда.
            HasUnsavedChanges = false;
            StatusText = $"Черновик сохранён автоматически: {path}. Нажмите «Сохранить проект», чтобы выбрать своё место.";
            return;
        }

        MarkSaved($"Сохранено автоматически: {DateTime.Now:HH:mm:ss}");
    }

    /// <summary>Файл для черновика, пока пользователь не выбрал своё место для проекта.</summary>
    private string BuildRecoveryPath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DocumentTemplates", "Черновики");

            Directory.CreateDirectory(dir);

            var name = SanitizeFileName(ProjectName);
            if (string.IsNullOrWhiteSpace(name)) name = "Черновик";

            return Path.Combine(dir, name + ".json");
        }
        catch (Exception)
        {
            return "";
        }
    }

    private bool WriteProjectFile(string path, out string error)
    {
        error = "";
        try
        {
            var project = new TemplateProjectJsonModel
            {
                Name = ProjectName,
                SourcePath = SourcePath,
                ResultPath = ResultPath,
                TemplateData = _templateData.ToJsonModel()
            };

            var json = JsonSerializer.Serialize(project, ProjectJsonOptions);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            // Пишем через временный файл: обрыв записи не должен уничтожить предыдущий проект.
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Copy(temp, path, overwrite: true);
            File.Delete(temp);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void MarkSaved(string message)
    {
        HasUnsavedChanges = false;
        StatusText = message;
        Log(message);
    }

    private void Fail(string message, Exception? ex = null)
    {
        StatusText = ex == null ? message : $"{message}: {ex.Message}";
        LogError(message, ex);
        Console.WriteLine($"[TU] {StatusText}");
    }

    // ------------------------------------------------------------------ совместимость со старыми файлами

    /// <summary>
    /// Совместимость со старыми/нестандартными JSON:
    /// если типы приборов лежат не в DeviceGroups[].Types, а строкой (TypeName/Type),
    /// восстанавливаем их в model.Types.
    /// </summary>
    private static void RestoreDeviceGroupTypesFromRawJson(string json, TemplateData loadedData)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement deviceGroupsEl;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("TemplateData", out var tdEl) &&
                tdEl.ValueKind == JsonValueKind.Object &&
                tdEl.TryGetProperty("DeviceGroups", out deviceGroupsEl) &&
                deviceGroupsEl.ValueKind == JsonValueKind.Array)
            {
                // ok
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("DeviceGroups", out deviceGroupsEl) &&
                     deviceGroupsEl.ValueKind == JsonValueKind.Array)
            {
                // ok
            }
            else
            {
                return;
            }

            var groups = loadedData.DeviceGroups;
            var sourceGroups = deviceGroupsEl.EnumerateArray().ToArray();
            var count = Math.Min(groups.Count, sourceGroups.Length);

            for (int i = 0; i < count; i++)
            {
                var group = groups[i];
                if (group == null) continue;

                var groupTypes = group.Types;
                if (groupTypes == null) continue;
                if (groupTypes.Count > 0) continue;

                var src = sourceGroups[i];
                if (src.ValueKind != JsonValueKind.Object) continue;

                if (TryGetStringIgnoreCase(src, "TypeName", out var typeName) ||
                    TryGetStringIgnoreCase(src, "Type", out typeName))
                {
                    var parts = (typeName ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s));

                    foreach (var part in parts)
                        groupTypes.Add(new DeviceType(part));
                }

                if (groupTypes.Count == 0 && TryGetPropertyIgnoreCase(src, "Types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in typesEl.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var n = item.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(n)) groupTypes.Add(new DeviceType(n));
                        }
                        else if (item.ValueKind == JsonValueKind.Object &&
                                 TryGetStringIgnoreCase(item, "Name", out var name))
                        {
                            var n = name?.Trim();
                            if (!string.IsNullOrWhiteSpace(n)) groupTypes.Add(new DeviceType(n));
                        }
                    }
                }
            }
        }
        catch
        {
            // Если структура JSON неожиданная, просто не применяем миграцию.
        }
    }

    private static bool TryGetStringIgnoreCase(JsonElement obj, string propertyName, out string? value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    value = prop.Value.GetString();
                    return true;
                }
                break;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string propertyName, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
