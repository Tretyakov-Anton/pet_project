using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Template_TU.Models;

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
        var group = templateData.Groups.FirstOrDefault(g => g.Name == GroupName);
        if (group == null) throw new InvalidOperationException($"Group '{GroupName}' not found");

        return new Element(Name)
        {
            Group = group,
            ParameterValues = new(ParameterValues.Select(p => p.ToParameterValue(group))),
            Description = Description,
        };
    }
}