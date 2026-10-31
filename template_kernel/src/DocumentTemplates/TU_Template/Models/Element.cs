using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TU_Template.Models;

public partial class Element(string name = "", string description = "") : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private Group? _group;
    [ObservableProperty] private ObservableCollection<ParameterValue> _parameterValues = new();
    [ObservableProperty] private string _description = description;

    partial void OnGroupChanged(Group? oldValue, Group? newValue)
    {
        oldValue?.Elements.Remove(this);
        newValue?.Elements.Add(this);

        ParameterValues = new(newValue?.ElectricalParameters.Select(p => new ParameterValue(p)) ?? [ ]);
    }

    public ElementJsonModel ToJsonModel() => new()
    {
        Name = Name,
        GroupName = Group?.Name,
        ParameterValues = ParameterValues.Select(p => p.ToJsomModel()).ToArray(),
        Description = Description,
    };
}

public class ElementJsonModel
{
    public string Name { get; set; } = "";
    public string? GroupName { get; set; }
    public ParameterValueJsonModel[] ParameterValues { get; set; } = [ ];
    public string Description { get; set; } = "";

    public Element ToElement(TemplateData templateData)
    {
        // Базовый элемент с именем и описанием
        var element = new Element(Name)
        {
            Description = Description
        };

        // Если группа не указана или не найдена — возвращаем элемент без привязки к группе
        if (string.IsNullOrWhiteSpace(GroupName))
            return element;

        var group = templateData.Groups.FirstOrDefault(g => g.Name == GroupName);
        if (group == null)
            return element;

        // Установка группы автоматически синхронизирует список параметров через OnGroupChanged
        element.Group = group;

        // Перенесем сохранённые значения границ, если такие параметры существуют в группе
        if (ParameterValues is { Length: > 0 })
        {
            foreach (var pv in element.ParameterValues)
            {
                var saved = ParameterValues.FirstOrDefault(s => s.ParameterName == pv.Parameter.Name);
                if (saved != null)
                {
                    pv.Limits = new(saved.AtLeast, saved.AtMost);
                }
            }
        }

        return element;
    }
}