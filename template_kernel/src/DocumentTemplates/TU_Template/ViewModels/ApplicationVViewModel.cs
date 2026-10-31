using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public class ApplicationVViewModel : ViewModelBase
{
    private readonly Window? _parentWindow;
    private readonly Models.TemplateData _templateData;

    /// <summary>Позиции, выбранные из справочника корпусов. В.1/В.2 заполняются по ним автоматически.</summary>
    public ObservableCollection<GchD2ItemViewModel> Items { get; } = new();

    /// <summary>Шифры раздела В.1, введённые вручную (поведение ветки attachmentC).</summary>
    public ObservableCollection<AttachedDocument> SectionB1 => _templateData.ApplicationVSectionB1;

    /// <summary>Шифры раздела В.2, введённые вручную (поведение ветки attachmentC).</summary>
    public ObservableCollection<AttachedDocument> SectionB2 => _templateData.ApplicationVSectionB2;

    public ReactiveCommand<Unit, Unit> ShowAddDialogCommand { get; }
    public ReactiveCommand<GchD2ItemViewModel, Unit> RemoveItemCommand { get; }
    public ReactiveCommand<GchD2ItemViewModel, Unit> EditItemCommand { get; }

    public ReactiveCommand<Unit, Unit> AddB1DocumentCommand { get; }
    public ReactiveCommand<Unit, Unit> AddB2DocumentCommand { get; }
    public ReactiveCommand<AttachedDocument, Unit> RemoveB1DocumentCommand { get; }
    public ReactiveCommand<AttachedDocument, Unit> RemoveB2DocumentCommand { get; }

    public ApplicationVViewModel(Window? parentWindow, Models.TemplateData templateData)
    {
        _parentWindow = parentWindow;
        _templateData = templateData;

        // Старые проекты хранили один корпус в AppendixVItem — поднимаем его в список.
        _templateData.NormalizeAppendixV();

        RebuildItemsFromModel();

        // Единственный источник истины для списка позиций — GchD2Items.
        _templateData.GchD2Items.CollectionChanged += (_, _) =>
        {
            RebuildItemsFromModel();
            _templateData.NormalizeAppendixV();
        };

        // Загрузка файла заменяет экземпляры коллекций — пересобираем представление.
        _templateData.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Models.TemplateData.GchD2Items)
                               or nameof(Models.TemplateData.ApplicationVSectionB1)
                               or nameof(Models.TemplateData.ApplicationVSectionB2))
            {
                RebuildItemsFromModel();
                this.RaisePropertyChanged(nameof(SectionB1));
                this.RaisePropertyChanged(nameof(SectionB2));
            }
        };

        ShowAddDialogCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (_parentWindow == null) return;

            var entries = LoadEntriesFromJson();
            if (entries.Count == 0) return;

            var dialog = new TU_Template.Views.AddGchDialog();
            dialog.SetEntries(entries);

            var result = await dialog.ShowDialog<bool?>(_parentWindow);

            if (result == true && !string.IsNullOrWhiteSpace(dialog.SelectedCaseName))
            {
                var caseName = dialog.SelectedCaseName ?? "";

                var isDuplicate = _templateData.GchD2Items.Any(x =>
                    string.Equals(x.CaseName, caseName, StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)
                {
                    Log($"Позиция \"{caseName}\" уже добавлена, пропущено.");
                    return;
                }

                _templateData.GchD2Items.Add(new Models.GchD2Item
                {
                    CaseName        = caseName,
                    UnifiedGch      = dialog.SelectedUnifiedGch      ?? "",
                    DescriptionCode = dialog.SelectedDescriptionCode ?? "",
                    DescriptionName = dialog.SelectedDescriptionName ?? "",
                });
            }
        });

        RemoveItemCommand = ReactiveCommand.Create<GchD2ItemViewModel>(item =>
        {
            if (item?.Model == null) return;
            _templateData.GchD2Items.Remove(item.Model);
            if (_templateData.GchD2Items.Count == 0)
                _templateData.AppendixVItem = null;
        });

        EditItemCommand = ReactiveCommand.CreateFromTask<GchD2ItemViewModel>(async item =>
        {
            if (_parentWindow == null || item?.Model == null) return;

            var entries = LoadEntriesFromJson();
            if (entries.Count == 0) return;

            var dialog = new TU_Template.Views.AddGchDialog();
            dialog.SetEntries(entries);

            if (await dialog.ShowDialog<bool?>(_parentWindow) != true) return;
            if (string.IsNullOrWhiteSpace(dialog.SelectedCaseName)) return;

            // Правим именно выбранную позицию, а не "основную".
            var index = _templateData.GchD2Items.IndexOf(item.Model);
            if (index < 0) return;

            _templateData.GchD2Items[index] = new Models.GchD2Item
            {
                CaseName        = dialog.SelectedCaseName        ?? "",
                UnifiedGch      = dialog.SelectedUnifiedGch      ?? "",
                DescriptionCode = dialog.SelectedDescriptionCode ?? "",
                DescriptionName = dialog.SelectedDescriptionName ?? "",
            };
        });

        AddB1DocumentCommand = ReactiveCommand.Create(() =>
            _templateData.ApplicationVSectionB1.Add(new AttachedDocument("ТЛВШ ")));

        AddB2DocumentCommand = ReactiveCommand.Create(() =>
            _templateData.ApplicationVSectionB2.Add(new AttachedDocument("ТЛВШ ")));

        RemoveB1DocumentCommand = ReactiveCommand.Create<AttachedDocument>(doc =>
        {
            if (doc != null) _templateData.ApplicationVSectionB1.Remove(doc);
        });

        RemoveB2DocumentCommand = ReactiveCommand.Create<AttachedDocument>(doc =>
        {
            if (doc != null) _templateData.ApplicationVSectionB2.Remove(doc);
        });
    }

    private void RebuildItemsFromModel()
    {
        Items.Clear();
        foreach (var item in _templateData.GchD2Items)
            Items.Add(new GchD2ItemViewModel(item));
    }

    private List<Views.AddGchDialog.GchEntry> LoadEntriesFromJson()
    {
        try
        {
            var baseDir    = AppDomain.CurrentDomain.BaseDirectory;
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? baseDir;

            var possiblePaths = new[]
            {
                Path.Combine(assemblyDir, "Resources", "Devices", "GCh_D2_List.json"),
                Path.Combine(assemblyDir, "GCh_D2_List.json"),
                Path.Combine(baseDir, "Resources", "Devices", "GCh_D2_List.json"),
                Path.Combine(baseDir, "GCh_D2_List.json"),
                Path.Combine(baseDir, "Templates", "Resources", "Devices", "GCh_D2_List.json"),
                Path.Combine(baseDir, "Templates", "GCh_D2_List.json"),
            };

            var jsonPath = possiblePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(jsonPath))
            {
                Console.WriteLine("GCh_D2_List.json не найден ни по одному из известных путей.");
                return new();
            }

            var json = File.ReadAllText(jsonPath);
            var rows = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>[]>(json);
            if (rows == null) return new();

            var list = new System.Collections.Generic.List<TU_Template.Views.AddGchDialog.GchEntry>();
            foreach (var row in rows)
            {
                row.TryGetValue("Название корпуса",     out var caseName);
                row.TryGetValue("Унифицированный ГЧ",   out var unifiedGch);
                row.TryGetValue("Описание внешнего вида", out var descCode);
                row.TryGetValue("Название описания",     out var descName);

                list.Add(new TU_Template.Views.AddGchDialog.GchEntry
                {
                    CaseName        = caseName    ?? string.Empty,
                    UnifiedGch      = unifiedGch  ?? string.Empty,
                    DescriptionCode = descCode    ?? string.Empty,
                    DescriptionName = descName    ?? string.Empty,
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки GCh_D2_List.json: {ex.Message}");
            return new();
        }
    }
}
