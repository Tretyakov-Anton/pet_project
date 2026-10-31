using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TU_Template.Models;

public partial class TemplateData : ObservableObject
{
    //В данном классе все данные
    [ObservableProperty] private ObservableCollection<Variable> _variables = new();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CollectionElements))]
    private ObservableLinkedCollection<TemplateData, Element> _elements;
   
    [ObservableProperty] private ObservableCollection<Group> _groups = new();
    [ObservableProperty]
    //Сюда записать
    //Инкапсуляцию использовать
    private ObservableCollection<DeviceGroup> _deviceGroups = new();
    
    [ObservableProperty]
    private ObservableCollection<TermDefinition> _termDefinitions = new();

    /// <summary>
    /// Корпус Приложения В, выбранный первым. Оставлен для совместимости со старыми
    /// файлами проектов и как «основная» позиция; полный список — в <see cref="GchD2Items"/>.
    /// </summary>
    [ObservableProperty]
    private AppendixVItem? _appendixVItem;

    /// <summary>Позиции Приложения В, выбранные из справочника корпусов. Может быть несколько.</summary>
    [ObservableProperty]
    private ObservableCollection<GchD2Item> _gchD2Items = new();

    /// <summary>
    /// Шифры документов раздела В.1 (габаритные чертежи), введённые вручную —
    /// в дополнение к позициям из справочника.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AttachedDocument> _applicationVSectionB1 = new();

    /// <summary>
    /// Шифры документов раздела В.2 (описания внешнего вида), введённые вручную —
    /// в дополнение к позициям из справочника.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AttachedDocument> _applicationVSectionB2 = new();

    public TemplateData()
    {
        _elements = new(this);
    }
    
    public IList<ObservableLinkedCollection<TemplateData, Element>.CollectionElement> CollectionElements => Elements;

    public void CopyFrom(TemplateData data)
    {
        // Сохраняем те же экземпляры коллекций, чтобы существующие ViewModel не теряли привязки.
        Variables.Clear();
        foreach (var v in data.Variables)
            Variables.Add(v);

        Groups.Clear();
        foreach (var g in data.Groups)
            Groups.Add(g);

        Elements.Clear();
        foreach (var e in data.Elements)
            Elements.Add(e);

        DeviceGroups.Clear();
        foreach (var dg in data.DeviceGroups)
            DeviceGroups.Add(dg);

        TermDefinitions.Clear();
        foreach (var t in data.TermDefinitions)
            TermDefinitions.Add(t);

        GchD2Items.Clear();
        foreach (var item in data.GchD2Items)
            GchD2Items.Add(item);

        ApplicationVSectionB1.Clear();
        foreach (var doc in data.ApplicationVSectionB1)
            ApplicationVSectionB1.Add(doc);

        ApplicationVSectionB2.Clear();
        foreach (var doc in data.ApplicationVSectionB2)
            ApplicationVSectionB2.Add(doc);

        this.AppendixVItem = data.AppendixVItem;

        NormalizeAppendixV();
    }

    /// <summary>
    /// Приводит данные Приложения В к единому виду после загрузки:
    /// старые проекты хранили один корпус в <see cref="AppendixVItem"/> — поднимаем его в список,
    /// а для новых проектов восстанавливаем <see cref="AppendixVItem"/> из первой позиции списка.
    /// </summary>
    public void NormalizeAppendixV()
    {
        if (GchD2Items.Count == 0 && AppendixVItem is { } single && !string.IsNullOrWhiteSpace(single.CaseName))
        {
            GchD2Items.Add(new GchD2Item
            {
                CaseName = single.CaseName,
                UnifiedGch = single.UnifiedGch,
                DescriptionCode = single.DescriptionCode,
                DescriptionName = single.DescriptionName,
            });
            return;
        }

        if (GchD2Items.Count > 0)
        {
            var first = GchD2Items[0];
            AppendixVItem = new AppendixVItem
            {
                CaseName = first.CaseName,
                UnifiedGch = first.UnifiedGch,
                DescriptionCode = first.DescriptionCode,
                DescriptionName = first.DescriptionName,
            };
        }
    }

    public TemplateDataJsonModel ToJsonModel() => new()
    {
        Variables = this.Variables.ToArray(),
        Groups = this.Groups.Select(g => g.ToJsonModel()).ToArray(),
        Elements = ((IList<Element>)this.Elements).Select(e => e.ToJsonModel()).ToArray(),
        DeviceGroups = this.DeviceGroups.ToArray(),
        TermDefinitions = this.TermDefinitions.ToArray(),
        AppendixVItem = this.AppendixVItem,
        GchD2Items = this.GchD2Items.ToArray(),
        ApplicationVSectionB1 = this.ApplicationVSectionB1.ToArray(),
        ApplicationVSectionB2 = this.ApplicationVSectionB2.ToArray(),
    };

    public void EnsureVariables(params string[] names)
    {
        if (names == null || names.Length == 0) return;
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!Variables.Any(v => v.Name == name))
            {
                Variables.Add(new Variable(name, ""));
            }
        }
    }
}


public class TemplateDataJsonModel
{
    public Variable[] Variables { get; set; } = [ ];
    public ElementJsonModel[] Elements { get; set; } = [ ];
    public GroupJsonModel[] Groups { get; set; } = [ ];
    public DeviceGroup[] DeviceGroups { get; set; } = [ ];
    public TermDefinition[] TermDefinitions { get; set; } = [ ];
    public AppendixVItem? AppendixVItem { get; set; }
    public GchD2Item[] GchD2Items { get; set; } = [ ];
    public AttachedDocument[] ApplicationVSectionB1 { get; set; } = [ ];
    public AttachedDocument[] ApplicationVSectionB2 { get; set; } = [ ];

    public TemplateData ToTemplateData()
    {
        var data = new TemplateData();
        data.Variables = new(Variables);
        data.Groups = new(Groups.Select(g => g.ToGroup()));
        data.Elements = new(data, Elements.Select(e => e.ToElement(data)));
        data.DeviceGroups = new(DeviceGroups);
        data.TermDefinitions = new(TermDefinitions);
        data.AppendixVItem = AppendixVItem;
        data.GchD2Items = new(GchD2Items ?? [ ]);
        data.ApplicationVSectionB1 = new(ApplicationVSectionB1 ?? [ ]);
        data.ApplicationVSectionB2 = new(ApplicationVSectionB2 ?? [ ]);
        return data;
    }
}
