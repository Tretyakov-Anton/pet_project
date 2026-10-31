using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Template_TU.Models;

public partial class ElectricalParameter(string name = "", string symbol = "", string temp = "") : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private string _symbol = symbol;
    [ObservableProperty] private string _temp = temp;
}

public partial class ParameterValue(ElectricalParameter parameter) : ObservableObject, IEquatable<ParameterValue>
{
    [ObservableProperty] private ValueLimits _limits = new();

    public ElectricalParameter Parameter { get; } = parameter;

    public ParameterValueJsonModel ToJsomModel() => new()
    {
        ParameterName = Parameter.Name,
        AtLeast = Limits.AtLeast,
        AtMost = Limits.AtMost,
    };


    public bool Equals(ParameterValue? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Limits, other.Limits) && ReferenceEquals(Parameter, other?.Parameter);
    }

    public override bool Equals(object? obj) => Equals(obj as ParameterValue);

    public override int GetHashCode() => HashCode.Combine(Limits, Parameter);
}

public class ParameterValueJsonModel
{
    public string ParameterName { get; set; } = "";
    public string AtLeast { get; set; } = "";
    public string AtMost { get; set; } = "";

    public ParameterValue ToParameterValue(Group group)
    {
        var parameter = group.ElectricalParameters.FirstOrDefault(g => g.Name == ParameterName);
        if (parameter == null) throw new InvalidOperationException($"Parameter '{ParameterName}' not found in group '{group.Name}'");

        return new ParameterValue(parameter)
        {
            Limits = new(AtLeast, AtMost)
        };
    }
}