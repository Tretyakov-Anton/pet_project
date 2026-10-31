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
    public DeviceType(string name)
    {
        Name = name;
    }

    public DeviceType() { }
}