using CommunityToolkit.Mvvm.ComponentModel;

namespace Template_TU.Models;

public partial class OperatingConditionsParameter(string name = "", string symbol = "") : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private string _symbol = symbol;
    [ObservableProperty] private ValueLimits _maximumPermissible = new();
    [ObservableProperty] private ValueLimits _limit = new();
    [ObservableProperty] private string _noteRefs = "";
}