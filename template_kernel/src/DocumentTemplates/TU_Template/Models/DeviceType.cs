using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
namespace TU_Template.Models;
public class DeviceType : ObservableObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    public bool IsCustom { get; set; } = true;
    public DeviceType(string name)
    {
        Name = name;
    }
    public int Index { get; set; }
    /// <summary>Конструктор для десериализации.</summary>
    [JsonConstructor]
    public DeviceType() : this("") { }
}