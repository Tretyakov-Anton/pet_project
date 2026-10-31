using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TU_Template.Models;

public partial class DeviceGroup : ObservableObject
{
    [ObservableProperty]
    private string _name;

    /// <summary>
    /// Типы (модели) прибора. Сеттер обязателен: System.Text.Json не умеет наполнять
    /// коллекцию, доступную только на чтение, и при загрузке проекта список типов терялся.
    /// </summary>
    public ObservableCollection<DeviceType> Types { get; set; } = new();

    public bool IsPredefined { get; set; }

    public RelayCommand AddTypeCommand { get; }
    public RelayCommand<DeviceType> RemoveTypeCommand { get; }

    public DeviceGroup(string name = "")
    {
        Name = name;
        AddTypeCommand = new RelayCommand(() =>
        {
            Types.Add(new DeviceType("Новый тип"));
        });

        RemoveTypeCommand = new RelayCommand<DeviceType>(type =>
        {
            if (type != null && Types.Contains(type))
                Types.Remove(type);
        });
    }

    /// <summary>Конструктор для десериализации: делегирует основному, чтобы команды не остались null.</summary>
    [JsonConstructor]
    public DeviceGroup() : this("") { }
}