using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TU_Template.Models;

public partial class ValueLimits(string atLeast = "", string atMost = "") : ObservableObject, IEquatable<ValueLimits>
{
    [ObservableProperty] private string _atLeast = atLeast;
    [ObservableProperty] private string _atMost = atMost;

    public bool Equals(ValueLimits? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return AtLeast == other.AtLeast && AtMost == other.AtMost;
    }

    public override bool Equals(object? obj) => Equals(obj as ValueLimits);

    public override int GetHashCode() => HashCode.Combine(AtLeast, AtMost);
}