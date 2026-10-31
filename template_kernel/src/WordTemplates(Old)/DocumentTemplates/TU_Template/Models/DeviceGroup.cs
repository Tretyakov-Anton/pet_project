using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TU_Template.Models;

public partial class DeviceGroup : ObservableObject
{
    [ObservableProperty]
    private string _name;

    public ObservableCollection<DeviceType> Types { get; } = new();

    public RelayCommand AddTypeCommand { get; }
    public RelayCommand<DeviceType> RemoveTypeCommand { get; }

    // 👇 ИСПРАВЛЕНО: конструктор с именем
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

    // 👇 Пустой конструктор для сериализации (если нужно)
    public DeviceGroup() { }
}